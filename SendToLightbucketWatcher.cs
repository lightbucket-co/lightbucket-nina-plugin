using Newtonsoft.Json;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System;

namespace Lightbucket.NINAPlugin { 
    public class SendToLightbucketWatcher
    {
        private HttpClient httpClient = new HttpClient();
        private IImageSaveMediator imageSaveMediator;
        private string LightbucketAPIBaseURL;
        private string LightbucketUsername;
        private string LightbucketAPIKey;
        private bool LightbucketEnabled;
        public SendToLightbucketWatcher(IImageSaveMediator imageSaveMediator)
        {
            this.imageSaveMediator = imageSaveMediator;
            LightbucketAPIBaseURL = $"{Properties.Settings.Default.LightbucketBaseURL}/api";
            LightbucketUsername = Properties.Settings.Default.LightbucketUsername;
            LightbucketAPIKey = Security.Decrypt(Properties.Settings.Default.LightbucketAPIKey);
            LightbucketEnabled = Properties.Settings.Default.LightbucketEnabled;

            Properties.Settings.Default.PropertyChanged += SettingsChanged;
            imageSaveMediator.ImageSaved += ImageSaveMeditator_ImageSaved;
        }

        public override string ToString()
        {
            return $"Category: Lightbucket, Item: {nameof(SendToLightbucketWatcher)}";
        }

        private async void ImageSaveMeditator_ImageSaved(object sender, ImageSavedEventArgs msg)
        {
            if (!LightbucketEnabled) { return; }
            if (msg.MetaData.Image.ImageType != "LIGHT") { return; }

            EquipmentPayload equipmentPayload = new EquipmentPayload
            {
                camera_name = msg.MetaData.Camera.Name,
                telescope_name = msg.MetaData.Telescope.Name
            };

            TargetPayload targetPayload = new TargetPayload
            {
                name = msg.MetaData.Target.Name,
                ra = msg.MetaData.Target.Coordinates.RADegrees,
                dec = msg.MetaData.Target.Coordinates.Dec,
                rotation = msg.MetaData.Target.Rotation
            };

            ImagePayload imagePayload = new ImagePayload
            {
                filter_name = msg.Filter,
                duration = msg.Duration,
                gain = msg.MetaData.Camera.Gain,
                offset = msg.MetaData.Camera.Offset,
                binning = msg.MetaData.Image.Binning?.ToString(),
                captured_at = DateTime.UtcNow,
                rms = msg.MetaData.Image.RecordedRMS.Total,
                thumbnail = CreateBase64Thumbnail(msg.Image)
            };

            LightbucketPayload payload = new LightbucketPayload
            {
                target = targetPayload,
                equipment = equipmentPayload,
                image = imagePayload
            };

            await MakeAPIRequest(payload);
        }

        private static string CreateBase64Thumbnail(BitmapSource source)
        {
            byte[] data = null;
            double scaleFactor = 300 / source.Width;
            BitmapSource resizedBitmap = new TransformedBitmap(source, new ScaleTransform(scaleFactor, scaleFactor));
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 70;
            encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                data = ms.ToArray();
            }

            return Convert.ToBase64String(data);
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
                case "LightbucketEnabled":
                    LightbucketEnabled = Properties.Settings.Default.LightbucketEnabled;
                    break;
            }
        }

        private class LightbucketPayload
        {
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
            public double rms { get; set; }
            public string thumbnail { get; set; }
        }
    }
}
