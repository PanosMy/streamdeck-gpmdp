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
    [PluginActionId("com.barraider.gpmdp.repeat")]
    class RepeatAction : ActionBase
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

        RepeatType repeat;

        #endregion

        public RepeatAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
                repeat = gpmdpManager.GetRepeat();
            }
            GpmdpClient.Instance.RepeatReceived += Instance_RepeatReceived;
        }

        #region Public Methods

        public override void Dispose()
        {
            GpmdpClient.Instance.RepeatReceived -= Instance_RepeatReceived;
            base.Dispose();
        }
        public override void KeyPressed(KeyPayload payload)
        {
            baseHandledKeypress = false;
            base.KeyPressed(payload);

            if (!baseHandledKeypress)
            {
                gpmdpManager.RepeatToggle();
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
                switch (repeat)
                {
                    case (RepeatType.None):
                    case (RepeatType.Unknown):
                        await Connection.SetImageAsync(Properties.Settings.Default.ImgRepeatOff);
                        break;
                    case (RepeatType.Single):
                        await Connection.SetImageAsync(Properties.Settings.Default.ImgRepeatTrack);
                        break;
                    case (RepeatType.List):
                        await Connection.SetImageAsync(Properties.Settings.Default.ImgRepeatOn);
                        break;
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

    private void Instance_RepeatReceived(object sender, RepeatType e)
    {
        repeat = e;
    }

    #endregion
}
}
