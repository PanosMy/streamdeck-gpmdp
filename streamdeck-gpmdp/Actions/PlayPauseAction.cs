using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using GPMDP_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Actions
{
    [PluginActionId("com.barraider.gpmdp.playpause")]
    public class PlayPauseAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.TokenExists = false;
                instance.ShowTimeElapsed = true;
                instance.ShowSongName = true;
                instance.ShowSongImage = true;
                return instance;
            }

            [JsonProperty(PropertyName = "showTimeElapsed")]
            public bool ShowTimeElapsed { get; set; }

            [JsonProperty(PropertyName = "showSongName")]
            public bool ShowSongName { get; set; }

            [JsonProperty(PropertyName = "showSongImage")]
            public bool ShowSongImage { get; set; }
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

        private bool isPlaying = false;
        private Track track;
        private int playtime;

        #endregion

        public PlayPauseAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

            GpmdpClient.Instance.PlayStateReceived += Client_PlayStateReceived;
            GpmdpClient.Instance.TimeReceived += Client_TimeReceived;
            GpmdpClient.Instance.TrackResultReceived += Client_TrackResultReceived;
        }

        #region Public Methods

        public override void Dispose()
        {
            GpmdpClient.Instance.PlayStateReceived -= Client_PlayStateReceived;
            GpmdpClient.Instance.TimeReceived -= Client_TimeReceived;
            GpmdpClient.Instance.TrackResultReceived -= Client_TrackResultReceived;
            base.Dispose();
        }

        public override void KeyPressed(KeyPayload payload)
        {
            baseHandledKeypress = false;
            base.KeyPressed(payload);

            if (!baseHandledKeypress)
            {
                if (!payload.IsInMultiAction ||
                    (payload.IsInMultiAction && payload.UserDesiredState == 1 && isPlaying) || // Multiaction mode, check if desired state is 1 (0==play, 1==pause)
                    (payload.IsInMultiAction && payload.UserDesiredState == 0 && !isPlaying))
                {
                    gpmdpManager.PlayPause();
                }
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
                await Connection.SetImageAsync((String)null);
            }
        }

        public override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        public override void KeyReleased(KeyPayload payload) { }
        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #endregion

        #region Private Methods

        private void Client_TrackResultReceived(object sender, GPMDP_Api.Models.Track e)
        {
            track = e;
        }

        private void Client_TimeReceived(object sender, GPMDP_Api.Models.TimeValues e)
        {
            playtime = e.Current;
        }

        private void Client_PlayStateReceived(object sender, bool e)
        {
            isPlaying = e;
        }


        #endregion

    }
}
