using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Actions
{
    public abstract class VolumeActionBase : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    VolumeParam = "10",
                    TokenExists = false,
                    ShowVolumeLevel = true
                };
                return instance;
            }

            [JsonProperty(PropertyName = "volumeParam")]
            public string VolumeParam { get; set; }

            [JsonProperty(PropertyName = "showVolumeLevel")]
            public bool ShowVolumeLevel { get; set; }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Protected Members

        //protected int currentVolume;

        #endregion

        public VolumeActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();

                if (this.GetType() == typeof(VolumeSetAction) || this.GetType() == typeof(VolumeMuteAction))
                {
                    Settings.VolumeParam = "50";
                }

                CheckTokenExists();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            CheckTokenExists();

            //GpmdpClient.Instance.VolumeReceived += Instance_VolumeReceived;
        }

        #region Public Methods

        public override void Dispose()
        {
            //GpmdpClient.Instance.VolumeReceived -= Instance_VolumeReceived;
            base.Dispose();
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
        }

        public override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }


        #endregion

        #region Private Methods
        /*
        private void Instance_VolumeReceived(object sender, int e)
        {
            currentVolume = e;
        }
        */
        #endregion 
    }
}
