using BarRaider.SdTools;
using GPMDP_Api;
using GPMDP_Api.Enums;
using GPMDP_Api.Models;
using GPMDP_Api.Playback;
using System;

namespace BarRaider.GPMDP.Communication
{
    internal class GpmdpClient
    {
        private static GpmdpClient instance = null;
        private static readonly object objLock = new object();

        private readonly Client client;

        #region Constructors

        public static GpmdpClient Instance
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
                        instance = new GpmdpClient();
                    }
                    return instance;
                }
            }
        }

        private GpmdpClient()
        {
            client = new Client(GpmdpSecret.APP_NAME);
            client.ConnectReceived += Client_ConnectReceived;
            client.OnError += Client_OnError;
            client.OnSocketError += Client_OnSocketError;
            client.QueueReceived += Client_QueueReceived;
            client.TrackResultReceived += Client_TrackResultReceived;
            client.ApiVersionReceived += Client_ApiVersionReceived;
            client.PlayStateReceived += Client_PlayStateReceived;
            client.VolumeReceived += Client_VolumeReceived;
            //client.LyricsReceived += Client_LyricsReceived;
            client.TimeReceived += Client_TimeReceived;
            client.ShuffleReceived += Client_ShuffleReceived;
            //client.RatingReceived += Client_RatingReceived;
            client.RepeatReceived += Client_RepeatReceived;
            //client.PlaylistsReceived += Client_PlaylistsReceived;
            //client.SearchResultsReceived += Client_SearchResultsReceived;
            //client.LibraryReceived += Client_LibraryReceived;
        }

        #endregion

        #region Public Events

        public event EventHandler<Track[]> QueueReceived;
        public event EventHandler<Track> TrackResultReceived;
        public event EventHandler<string> ApiVersionReceived;
        public event EventHandler<bool> PlayStateReceived;
        public event EventHandler<int> VolumeReceived;
        //public event EventHandler<string> LyricsReceived;
        public event EventHandler<TimeValues> TimeReceived;
        public event EventHandler<ShuffleType> ShuffleReceived;
        //public event EventHandler<LikedValues> RatingReceived;
        public event EventHandler<RepeatType> RepeatReceived;
        //public event EventHandler<Playlist[]> PlaylistsReceived;
        //public event EventHandler<Results> SearchResultsReceived;
        //public event EventHandler<Contents> LibraryReceived;

        #endregion

        #region Public Methods

        public bool IsAuthenticated { get; private set; }
        public bool IsConnected
        {
            get
            {
                return client.IsConnected;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return IsConnected && client.IsPlaying();
            }
        }

        public void Connect()
        {
            if (!client.IsConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Client attempting to connect");
                IsAuthenticated = false;
                client.Connect();
            }
        }
    
        public void Authenticate(string approvalCode)
        {
            if (client.IsConnected)
            {
                try
                {
                    client.Authenticate(approvalCode);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to authenticate: {ex}");
                }
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Cannot authenticate - Client not connected (Is GPMDP up and running?)");
        }

        #endregion

        #region Private Methods

        internal Client GetClient()
        {
            return client;
        }


        private void Client_OnError(object sender, string e)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Client Error: {e}");
        }

        private void Client_OnSocketError(object sender, SocketErrorException e)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Client Socket Error! Message: {e.Message} Exception: {e.Exception}");
        }

        private void Client_ConnectReceived(object sender, string e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ConnectReceived {e}");

            if (e != Client.RESPONSE_CODE_REQUIRED)
            {
                IsAuthenticated = true;
                Authenticate(e);
                TokenManager.Instance.InitTokens(e, DateTime.Now);
            }
            else
            {
                IsAuthenticated = false;
                TokenManager.Instance.InitTokens(null, DateTime.Now);
            }
        }

        private void Client_RepeatReceived(object sender, RepeatType e)
        {
            RepeatReceived?.Invoke(this, e);
        }

        private void Client_ShuffleReceived(object sender, ShuffleType e)
        {
            ShuffleReceived?.Invoke(this, e);
        }

        private void Client_TimeReceived(object sender, TimeValues e)
        {
            TimeReceived?.Invoke(this, e);
        }

        private void Client_VolumeReceived(object sender, int e)
        {
            VolumeReceived?.Invoke(this, e);
        }

        private void Client_PlayStateReceived(object sender, bool e)
        {
            PlayStateReceived?.Invoke(this, e);
        }

        private void Client_ApiVersionReceived(object sender, string e)
        {
            if (!String.IsNullOrEmpty(e))
            {
                IsAuthenticated = true;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"API Version: {e}");
            ApiVersionReceived?.Invoke(this, e);
        }

        private void Client_TrackResultReceived(object sender, Track e)
        {
            TrackResultReceived?.Invoke(this, e);
        }

        private void Client_QueueReceived(object sender, Track[] e)
        {
            QueueReceived?.Invoke(this, e);
        }


        #endregion
    }
}
