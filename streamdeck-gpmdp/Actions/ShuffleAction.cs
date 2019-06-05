using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using GPMDP_Api.Enums;
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
                PluginSettings instance = new PluginSettings();
                instance.TokenExists = false;
                return instance;
            }
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

            if (gpmdpManager.IsConnected)
            {
                shuffle = gpmdpManager.GetShuffle();
            }
            GpmdpClient.Instance.ShuffleReceived += Client_ShuffleReceived;
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
                gpmdpManager.ShuffleToggle();
            }
        }
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
        }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (!baseHandledOnTick)
            {
                if (shuffle == ShuffleType.All)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgShuffleOn);
                }
                else
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.ImgShuffleOff);
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

        #endregion
    }
}
