using BarRaider.SdTools;
using GPMDP_Api;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    internal class TokenManager
    {
        #region Private Members
        private static TokenManager instance = null;
        private static readonly object objLock = new object();

        private GpmdpToken token;
        private GlobalSettings global;

        #endregion

        #region Public Members

        public event EventHandler<GpmdpTokenEventArgs> TokensChanged;
        #endregion

        #region Constructors

        public static TokenManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new TokenManager();
                    }
                    return instance;
                }
            }
        }

        private TokenManager()
        {
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Instance_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            
        }

        #endregion

        #region Public Methods

        public bool TokenExists
        {
            get
            {
                return (token != null && !string.IsNullOrWhiteSpace(token.AccessToken) && GpmdpClient.Instance.IsAuthenticated);
            }
        }

        public bool Authenticate()
        {
            if (TokenExists) // Already authenticated
            {
                return true;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, "Attempting to authenticate");
            if (token != null && !string.IsNullOrWhiteSpace(token.AccessToken))
            {
                GpmdpClient.Instance.Connect();
                GpmdpClient.Instance.Authenticate(token.AccessToken);
                return true;
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot authenticate, invalid token");
            }
            return false;
        }

        internal void InitTokens(string accessToken, DateTime tokenCreateDate)
        {
            if (token == null || token.TokenLastRefresh < tokenCreateDate)
            {
                if (String.IsNullOrWhiteSpace(accessToken))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "InitTokens: Token revocation!");
                }
                token = new GpmdpToken() { AccessToken = accessToken, TokenLastRefresh = tokenCreateDate };
                SaveToken();
            }
            if (token != null && !string.IsNullOrWhiteSpace(token.AccessToken))
            {
                GpmdpClient.Instance.Connect();
                Authenticate();
            }
            TokensChanged?.Invoke(this, new GpmdpTokenEventArgs(TokenExists));
        }

        #endregion

        #region Private Methods

        private void LoadToken(GpmdpToken globalToken)
        {
            try
            {
                if (globalToken == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to load tokens, deserialized globalToken is null");
                    return;
                }
                token = new GpmdpToken()
                {
                    AccessToken = globalToken.AccessToken,
                    TokenLastRefresh = globalToken.TokenLastRefresh
                };

                Logger.Instance.LogMessage(TracingLevel.INFO, $"Token initialized. Last refresh date was: {token.TokenLastRefresh}");
                InitTokens(token.AccessToken, token.TokenLastRefresh);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception loading tokens: {ex}");
            }
        }

        private void SaveToken()
        {
            try
            {
                if (global == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to save token, Global Settings is null");
                    return;
                }

                // Set token in Global Settings
                if (token == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Saving null token to Global Settings");
                    global.Token = null;
                }
                else
                {
                    global.Token = new GpmdpToken()
                    {
                        AccessToken = token.AccessToken,
                        TokenLastRefresh = token.TokenLastRefresh
                    };
                }

                GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(global));
                Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved. Last refresh date was: {token.TokenLastRefresh}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception saving tokens: {ex}");
            }
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null && payload.Settings.Count > 0)
            {
                global = payload.Settings.ToObject<GlobalSettings>();
                LoadToken(global.Token);
            }
            else // First time
            {
                global = new GlobalSettings();
                GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(global));
            }
        }

        #endregion
    }
}
