using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.SequenceItem.Imaging;
using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Lightbucket.NINAPlugin
{
    [ExportMetadata("Name", "Send to Lightbucket")]
    [ExportMetadata("Description", "This trigger will send a notification to Lightbucket any time an image capture is completed.")]
    [ExportMetadata("Icon", "StarSVG")]
    [ExportMetadata("Category", "Lightbucket")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SendToLightbucketTrigger : SequenceTrigger
    {
        private HttpClient httpClient = new HttpClient();
        private TakeExposure lastExposureItem;
        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private string LightbucketAPIBaseURL;
        private string LightbucketUsername;
        private string LightbucketAPIKey;

        [ImportingConstructor]
        public SendToLightbucketTrigger(
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IFilterWheelMediator filterWheelMediator
        )
        {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            LightbucketAPIBaseURL = $"{Properties.Settings.Default.LightbucketBaseURL}/api";
            LightbucketUsername = Properties.Settings.Default.LightbucketUsername;
            LightbucketAPIKey = Security.Decrypt(Properties.Settings.Default.LightbucketAPIKey);

            Properties.Settings.Default.PropertyChanged += SettingsChanged;
        }

        public override object Clone()
        {
            return new SendToLightbucketTrigger(profileService, cameraMediator, filterWheelMediator)
            {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description
            };
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            try
            {
                // 1. Get Target
                // 2. Get Equipment Info
                // 3. Get Image Info
                DeepSkyObject targetContainer = FindDsoInfo(lastExposureItem.Parent);

                if (targetContainer == null)
                {
                    Logger.Warning($"{this}: Could not identify target.  Skipping.");
                    return;
                }

                var targetPayload = new TargetPayload
                {
                    name = targetContainer.Name,
                    ra = targetContainer.Coordinates.RADegrees,
                    dec = targetContainer.Coordinates.Dec,
                    rotation = targetContainer.Rotation
                };

                var equipmentPayload = new EquipmentPayload
                {
                    camera_name = cameraMediator.GetInfo().Name,
                    telescope_name = profileService.ActiveProfile.TelescopeSettings.Name
                };

                var gain = lastExposureItem.Gain;
                if (gain == -1)
                {
                    gain = cameraMediator.GetInfo().Gain;
                }

                var offset = lastExposureItem.Offset;
                if (offset == -1)
                {
                    offset = cameraMediator.GetInfo().Offset;
                }

                var imagePayload = new ImagePayload
                {
                    filter_name = filterWheelMediator?.GetInfo()?.SelectedFilter?.Name,
                    duration = lastExposureItem.ExposureTime,
                    gain = gain,
                    offset = offset,
                    binning = lastExposureItem.Binning?.ToString(),
                    captured_at = DateTime.UtcNow
                };

                var payload = new LightbucketPayload
                {
                    target = targetPayload,
                    equipment = equipmentPayload,
                    image = imagePayload
                };


                await MakeAPIRequest(payload);
            }
            finally
            {
                lastExposureItem = null;
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            return false;
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            bool isFinishedExposure = previousItem?.GetType().Name == "TakeExposure" &&
                                      previousItem?.Status == SequenceEntityStatus.FINISHED;

            if (!isFinishedExposure) { return false; }

            try
            {
                TakeExposure exposureItem = (TakeExposure)previousItem;

                if (exposureItem.ImageType == "LIGHT")
                {
                    lastExposureItem = exposureItem;
                    return true;
                }
            } catch (InvalidCastException)
            {
                Logger.Trace($"{this}: Something went wrong casting to exposure item. previousItem was a {previousItem.GetType().Name}");
                return false;
            }

            return false;
        }

        public override string ToString()
        {
            return $"Category: {Category}, Item: {nameof(SendToLightbucketTrigger)}";
        }

        private async Task MakeAPIRequest(LightbucketPayload payload)
        {
            string jsonPayload = JsonConvert.SerializeObject(payload);
            Logger.Trace($"{this}: Making API request to {LightbucketAPIBaseURL}/image_capture_complete with payload: {jsonPayload}");

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{LightbucketAPIBaseURL}/image_capture_complete"
            );

            string authenticationString = $"{LightbucketUsername}:{LightbucketAPIKey}";
            string base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage apiResponse = await httpClient.SendAsync(request);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    Notification.ShowWarning($"{this}: API request failed with status {apiResponse.StatusCode}");
                    Logger.Warning($"{this}: API request failed with status {apiResponse.StatusCode}");
                }
                else
                {
                    Logger.Trace($"{this}: API request successful.");
                }
            }
            catch (HttpRequestException e)
            {
                Notification.ShowError($"{this}: {e.InnerException.Message}");
                Logger.Warning($"{this}: {e.InnerException.Message}");
            }
        }

        private DeepSkyObject FindDsoInfo(ISequenceContainer container)
        {
            DeepSkyObject target = null;
            ISequenceContainer acontainer = container;

            while (acontainer != null)
            {
                if (acontainer is IDeepSkyObjectContainer dsoContainer)
                {
                    target = dsoContainer.Target.DeepSkyObject;
                    break;
                }

                acontainer = acontainer.Parent;
            }

            return target;
        }
        void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "LightbucketUsername":
                    LightbucketUsername = Properties.Settings.Default.LightbucketUsername;
                    break;
                case "LightbucketAPIKey":
                    LightbucketAPIKey = Security.Decrypt(Properties.Settings.Default.LightbucketAPIKey);
                    break;
            }
        }

        private class LightbucketPayload {
            public TargetPayload target { get; set; }
            public EquipmentPayload equipment { get; set; }
            public ImagePayload image { get; set; }
        }

        private class TargetPayload
        {
            public string name { get; set; }
            public double ra { get; set; }
            public double dec { get; set; }
            public double rotation { get; set; }
        }

        private class EquipmentPayload
        {
            public string camera_name { get; set; }
            public string telescope_name { get; set; }
        }

        private class ImagePayload
        {
            public string filter_name { get; set; }
            public int gain { get; set; }
            public int offset { get; set; }
            public double duration { get; set; }
            public string binning { get; set; }
            public DateTime captured_at { get; set; }
        }
    }
}
