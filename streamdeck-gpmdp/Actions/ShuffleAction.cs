using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using GPMDP_Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Actions
{
    [PluginActionId("com.barraider.gpmdp.shuffle")]
    class ShuffleAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ShuffleOffImage = String.Empty,
                    ShuffleOnImage = String.Empty
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "shuffleOffImage")]
            public string ShuffleOffImage { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "shuffleOnImage")]
            public string ShuffleOnImage { get; set; }
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

        #region Private Members

        ShuffleType shuffle;
        private string shuffleOnBase64ImageStr = null;
        private string shuffleOffBase64ImageStr = null;

        #endregion

        public ShuffleAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();
                CheckTokenExists();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            CheckTokenExists();

            if (gpmdpManager.IsReadyForCommand)
            {
                shuffle = gpmdpManager.GetShuffle().GetAwaiter().GetResult();
            }
            GpmdpClient.Instance.ShuffleReceived += Client_ShuffleReceived;
            LoadCustomImages();
        }

        #region Public Methods

        public override void Dispose()
        {
            GpmdpClient.Instance.ShuffleReceived -= Client_ShuffleReceived;
            base.Dispose();
        }

        public override void KeyPressed(KeyPayload payload)
        {
            baseHandledKeypress = false;
            base.KeyPressed(payload);

            if (!baseHandledKeypress)
            {
                

                if (!payload.IsInMultiAction)
                {
                    gpmdpManager.ShuffleToggle();
                }
                else if (payload.UserDesiredState == 1) // Multiaction mode, check if desired state is 1 (0==shuffle, 1==no shuffle) 
                {
                    gpmdpManager.SetShuffle(ShuffleType.None);
                }
                else // UserDesiredState == 0
                {
                    gpmdpManager.SetShuffle(ShuffleType.All);
                }

            }
        }
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            LoadCustomImages();
            SaveSettings();
        }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (!baseHandledOnTick)
            {
                if (shuffle == ShuffleType.All)
                {
                    if (!String.IsNullOrEmpty(shuffleOnBase64ImageStr))
                    {
                        await Connection.SetImageAsync(shuffleOnBase64ImageStr);
                    }
                    else
                    {
                        await Connection.SetImageAsync(Properties.Settings.Default.ImgShuffleOn);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(shuffleOffBase64ImageStr))
                    {
                        await Connection.SetImageAsync(shuffleOffBase64ImageStr);
                    }
                    else
                    {
                        await Connection.SetImageAsync(Properties.Settings.Default.ImgShuffleOff);
                    }
                }
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion

        #region Private Methods

        private void Client_ShuffleReceived(object sender, GPMDP_Api.Enums.ShuffleType e)
        {
            shuffle = e;
        }

        private void LoadCustomImages()
        {
            shuffleOnBase64ImageStr = Tools.FileToBase64(Settings.ShuffleOnImage, true);
            shuffleOffBase64ImageStr = Tools.FileToBase64(Settings.ShuffleOffImage, true);
        }

        #endregion
    }
}
