using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Actions
{
    public abstract class ActionBase : PluginBase
    {
        protected class PluginSettingsBase
        {
            [JsonProperty(PropertyName = "tokenExists")]
            public bool TokenExists { get; set; }
        }

        #region Protected Members

        protected PluginSettingsBase settings;
        protected GpmdpManager gpmdpManager;
        protected bool baseHandledKeypress = false;
        protected bool baseHandledOnTick = false;
        protected string keyBase64ImageStr = null;
        private bool isAuthenticating = false;

        #endregion

        public ActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            gpmdpManager = new GpmdpManager();
            gpmdpManager.TokenStatusChanged += GpmdpManager_TokenStatusChanged;
            //CurrentlyPlayingManager.Instance.PlaybackInfoChanged += CurrentlyPlayingManager_PlaybackInfoChanged;
            Connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
        }


        #region Public Methods

        public virtual Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        public override void Dispose()
        {
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            //CurrentlyPlayingManager.Instance.PlaybackInfoChanged -= CurrentlyPlayingManager_PlaybackInfoChanged;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Base Destructor Called");
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Keypress: {GetType()}");
            if (!gpmdpManager.TokenExists || !gpmdpManager.IsClientConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Keypress - GPMDP is not connected");
                baseHandledKeypress = true;
                await Connection.ShowAlert();
                gpmdpManager.ForceConnect();
            }
        }

        public async override void OnTick()
        {
            string base64Image = GetBasicImage();
            if (!string.IsNullOrWhiteSpace(base64Image))
            {
                baseHandledOnTick = true;
                await Connection.SetImageAsync(base64Image);
            }
        }

        #endregion

        #region Private Methods

        private string GetBasicImage()
        {
            if (!settings.TokenExists)
            {
                return Properties.Settings.Default.ImgNoToken;
            }

            if (!CurrentlyPlayingManager.Instance.IsConnected)
            {
                return Properties.Settings.Default.ImgNoConnection;
            }

            if (keyBase64ImageStr != null)
            {
                return keyBase64ImageStr;
            }

            return null;
        }

        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "initiateauthentication":
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Initiating Authentication with GPMDP");
                        gpmdpManager.SubmitApprovalCode(null);
                        break;
                    case "updateapproval":
                        string approvalCode = ((string)payload["approvalCode"]).Trim();
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Requesting approval with code: {approvalCode}");
                        isAuthenticating = true;
                        gpmdpManager.SubmitApprovalCode(approvalCode);
                        break;
                    case "resetplugin":
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"ResetPlugin called. Tokens are cleared");
                        TokenManager.Instance.InitTokens(null, DateTime.Now);
                        await SaveSettings();
                        break;
                }
            }
        }

        private void GpmdpManager_TokenStatusChanged(object sender, EventArgs e)
        {
            CheckTokenExists();
        }

        protected void CheckTokenExists()
        {
            bool tokenExists = gpmdpManager.TokenExists;

            if (isAuthenticating)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"RefreshToken completed. Token Exists: {tokenExists}");
            }

            if (isAuthenticating || tokenExists != settings.TokenExists)
            {
                isAuthenticating = false;    
                settings.TokenExists = tokenExists;
                SaveSettings();
            }
        }

        #endregion
    }
}
