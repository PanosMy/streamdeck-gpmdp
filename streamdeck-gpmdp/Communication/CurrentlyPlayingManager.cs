using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    public class CurrentlyPlayingManager
    {
        #region Private Members
        private static CurrentlyPlayingManager instance = null;
        private static readonly object objLock = new object();

        private static readonly string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private const int RETRY_COUNT = 3;
        private const string PLAYBACK_INFO_FILE = @"Google Play Music Desktop Player\json_store\playback.json";

        private bool getPlaybackInfo = false;
        private PlaybackInfo lastPlaybackInfo;
        private DateTime lastSongFetch;
        private object backgroundWorkerLock = new object();

        #endregion

        #region Constructors

        public static CurrentlyPlayingManager Instance
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
                        instance = new CurrentlyPlayingManager();
                    }
                    return instance;
                }
            }
        }

        private CurrentlyPlayingManager()
        {
            IsConnected = false;
            StartBackgroundWorker();
        }

        #endregion

        #region Public Events

        public event EventHandler<PlaybackInfoEventArgs> PlaybackInfoChanged;
        public bool IsConnected { get; private set; }
        internal bool BackgroundWorkerRunning { get; private set; }
        #endregion

        #region Public Methods

        public void SpotifyForceFetchCurrentlyPlaying()
        {
            PlaybackInfoChanged?.Invoke(this, new PlaybackInfoEventArgs(lastPlaybackInfo));
        }
        public bool ForceConnect()
        {
            if (!TokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot Force connect - invalid token");
                return false;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, "Force Connect called, attempting to reconnect");
            StartBackgroundWorker();
            return true;
        }

        #endregion

        #region Private Methods

        private void StartBackgroundWorker()
        {
            return;
            lock (backgroundWorkerLock)
            {
                getPlaybackInfo = true;
                if (!BackgroundWorkerRunning)
                {
                    Task.Run(() => PlaybackInfoBackgroundWorker());
                }
            }
        }

        private void PlaybackInfoBackgroundWorker()
        {
            int retries = RETRY_COUNT;
            Logger.Instance.LogMessage(TracingLevel.INFO, "PlaybackInfoBackgroundWorker Start");
            while (getPlaybackInfo)
            {
                lock (backgroundWorkerLock)
                {
                    BackgroundWorkerRunning = true;
                }

                // No point in pinging Spotify if the plugin isn't running
                if (PlaybackInfoChanged == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                string playbackInfoFilename = Path.Combine(appDataFolder, PLAYBACK_INFO_FILE);
                if (!File.Exists(playbackInfoFilename))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"PlaybackInfoBackgroundWorker: File not found {playbackInfoFilename}");
                    getPlaybackInfo = false;
                    continue;
                }

                // File exists
                DateTime playbackLastRefresh = DateTime.Now;
                lastPlaybackInfo = JsonConvert.DeserializeObject<PlaybackInfo>(File.ReadAllText(playbackInfoFilename));
                if (lastPlaybackInfo != null)
                {
                    IsConnected = true;
                    lastPlaybackInfo.LastPlaybackRefresh = playbackLastRefresh;
                    PlaybackInfoChanged?.Invoke(this, new PlaybackInfoEventArgs(lastPlaybackInfo));
                }
                else // Could not parse file
                {

                    Logger.Instance.LogMessage(TracingLevel.WARN, $"PlaybackInfoBackgroundWorker failed to parse {playbackInfoFilename} Retries: {retries}");

                    IsConnected = false;
                    retries--;
                    if (retries <= 0)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "PlaybackInfoBackgroundWorker Failed - No retries left");
                        getPlaybackInfo = false;
                        continue;
                    }
                }

                Thread.Sleep(500);
            } // of While loop

            lock (backgroundWorkerLock)
            {
                BackgroundWorkerRunning = false;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, "PlaybackInfoBackgroundWorker Exited");
        }

        #endregion

        #region Fetch Album Art

        /*
        private bool ShouldFetchSongInfo()
        {
            if (CurrentlyPlayingChanged == null)
            {
                return false;
            }

            TimeSpan t = TimeSpan.FromMilliseconds(lastPlaybackInfo.Progress);
            if (t.TotalSeconds <= 1 || ((DateTime.Now - lastSongFetch).TotalSeconds > t.TotalSeconds))
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ShouldFetchSong: Seconds: {t.Seconds} LastRefresh: {(DateTime.Now - lastSongFetch).TotalSeconds}");
                return true;
            }

            return false;
        }

        private async void SpotifyFetchCurrentlyPlaying()
        {
            if (ShouldFetchSongInfo())
            {
                var response = await comm.SpotifyQuery(SPOTIFY_URI_SONG_PLAYING, SendMethod.GET, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    lastSongFetch = DateTime.Now;
                    try
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        SpotifyCurrentlyPlaying currentPlaying = JsonConvert.DeserializeObject<SpotifyCurrentlyPlaying>(body);
                        string url = String.Empty;
                        if (currentPlaying != null)
                        {
                            if (currentPlaying?.Track?.Album?.ImagesInfo?.Length > 1)
                            {
                                url = currentPlaying.Track.Album.ImagesInfo.OrderBy(x => x.Height).Skip(1).First().Url;
                            }
                            else if (currentPlaying?.Track?.Album?.ImagesInfo?.Length == 1)
                            {
                                url = currentPlaying.Track.Album.ImagesInfo.First().Url;
                            }


                            if (!string.IsNullOrEmpty(url))
                            {
                                currentPlaying.Track.AlbumImage = FetchImage(url);
                            }
                        }
                        lastCurrentlyPlaying = currentPlaying;
                        CurrentlyPlayingChanged?.Invoke(this, new CurrentlyPlayingEventArgs(currentPlaying));
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Fetched song info: {currentPlaying.Track.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"SpotifyCurrentSong Exception: {ex}");
                    }
                }
            }
        }

        private Bitmap FetchImage(string imageUrl)
        {
            if (String.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            WebClient client = new WebClient();
            Stream stream = client.OpenRead(imageUrl);
            Bitmap image = new Bitmap(stream);
            return image;
        }
        */
        #endregion
    }
}
