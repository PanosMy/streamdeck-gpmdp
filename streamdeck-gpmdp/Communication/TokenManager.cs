using BarRaider.SdTools;
using GPMDP_Api;
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
        private const string TOKEN_FILE = "gpmdp.dat";

        private static TokenManager instance = null;
        private static readonly object objLock = new object();

        private GpmdpToken token;
        private object refreshTokensLock = new object();

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
            LoadToken();
            if (token != null && !string.IsNullOrWhiteSpace(token.AccessToken))
            {
                GpmdpClient.Instance.Connect();
                Authenticate();
            }
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
                Authenticate();
            }
            TokensChanged?.Invoke(this, new GpmdpTokenEventArgs(TokenExists));
        }

        #endregion

        #region Private Methods

        private void LoadToken()
        {
            try
            {
                string fileName = Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE);
                if (File.Exists(fileName))
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        var formatter = new BinaryFormatter();
                        token = (GpmdpToken)formatter.Deserialize(stream);
                        if (token == null)
                        {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to load tokens, deserialized token is null");
                            return;
                        }
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Token initialized. Last refresh date was: {token.TokenLastRefresh}");
                    }
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to load tokens, token file does not exist: {fileName}");
                }
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
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE), FileMode.Create, FileAccess.Write))
                {

                    formatter.Serialize(stream, token);
                    stream.Close();
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved. Last refresh date was: {token.TokenLastRefresh}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception saving tokens: {ex}");
            }
        }

        #endregion
    }
}
