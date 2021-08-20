using System.Windows;
using System.ComponentModel.Composition;

namespace Lightbucket.NINAPlugin
{
    [Export(typeof(ResourceDictionary))]
    public partial class SendToLightbucketTriggerTemplate : ResourceDictionary
    {
        public SendToLightbucketTriggerTemplate()
        {
            this.InitializeComponent();
        }
    }
}
