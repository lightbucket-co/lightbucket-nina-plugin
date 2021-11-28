using Lightbucket.NINAPlugin.Properties;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Lightbucket.NINAPlugin
{
    /// <summary>
    /// This class exports the IPluginManifest interface and will be used for the general plugin information and options
    /// The base class "PluginBase" will populate all the necessary Manifest Meta Data out of the AssemblyInfo attributes. Please fill these accoringly
    /// 
    /// An instance of this class will be created and set as datacontext on the plugin options tab in N.I.N.A. to be able to configure global plugin settings
    /// The user interface for the settings will be defined by a DataTemplate with the key having the naming convention "<MyPlugin.Name>_Options" where MyPlugin.Name corresponds to the AssemblyTitle - In this template example it is found in the Options.xaml
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class LightbucketPlugin : PluginBase, INotifyPropertyChanged
    {
        SendToLightbucketWatcher watcher;

        [ImportingConstructor]
        public LightbucketPlugin(IImageSaveMediator imageSaveMediator)
        {
            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                Settings.Default.Save();
            }

            this.watcher = new SendToLightbucketWatcher(imageSaveMediator);
        }

        public string LightbucketUsername
        {
            get
            {
                return Settings.Default.LightbucketUsername;
            }
            set
            {
                Settings.Default.LightbucketUsername = value.Trim();
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }
        public string LightbucketAPIKey
        {
            get => Security.Decrypt(Settings.Default.LightbucketAPIKey);
            set
            {
                Settings.Default.LightbucketAPIKey = Security.Encrypt(value.Trim());
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public string LightbucketBaseURL
        {
            get => Settings.Default.LightbucketBaseURL;
        }

        public string LightbucketAPICredentialsURL
        {
            get => $"{Settings.Default.LightbucketBaseURL}/api_credentials";
        }

        public bool LightbucketEnabled
        {
            get => Settings.Default.LightbucketEnabled;
            set 
            {
                Settings.Default.LightbucketEnabled = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
