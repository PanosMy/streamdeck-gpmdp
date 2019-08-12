using BarRaider.GPMDP.Communication;
using BarRaider.SdTools;
using GPMDP_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // 100 Bits: lostlocalhost
    //---------------------------------------------------

    [PluginActionId("com.barraider.gpmdp.playpause")]
    public class PlayPauseAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ShowTimeElapsed = true,
                    ShowSongName = true,
                    ShowSongImage = true,
                    PlayImage = String.Empty,
                    PauseImage = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "showTimeElapsed")]
            public bool ShowTimeElapsed { get; set; }

            [JsonProperty(PropertyName = "showSongName")]
            public bool ShowSongName { get; set; }

            [JsonProperty(PropertyName = "showSongImage")]
            public bool ShowSongImage { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "playImage")]
            public string PlayImage { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "pauseImage")]
            public string PauseImage { get; set; }
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

        private Track track;
        private int progress;
        private string lastAlbumArtUrl = string.Empty;
        private Bitmap albumImage = null;
        private readonly Image imgPlayPauseDefault = Tools.Base64StringToImage(Properties.Settings.Default.ImgPlayPause);
        private string trackTitle = null;
        private StringBuilder trackTitleShifted = null;
        private Image imgCustomizedPlay = null;
        private Image imgCustomizedPause = null;

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

            GpmdpClient.Instance.TimeReceived += Client_TimeReceived;
            GpmdpClient.Instance.TrackResultReceived += Client_TrackResultReceived;
            LoadCustomImages();
        }

        #region Public Methods

        public override void Dispose()
        {
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
                    (payload.IsInMultiAction && payload.UserDesiredState == 1 && gpmdpManager.IsPlaying) || // Multiaction mode, check if desired state is 1 (0==play, 1==pause)
                    (payload.IsInMultiAction && payload.UserDesiredState == 0 && !gpmdpManager.IsPlaying))
                {
                    gpmdpManager.PlayPause();
                }
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) 
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            LoadCustomImages();
            SaveSettings();
        }

        public override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (!baseHandledOnTick)
            {
                DrawPlayPauseKey();
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

        private async void Client_TrackResultReceived(object sender, GPMDP_Api.Models.Track e)
        {
            track = e;
            if (track != null)
            {
                if (!String.IsNullOrEmpty(track.albumArt) && track.albumArt != lastAlbumArtUrl)
                {
                    lastAlbumArtUrl = track.albumArt;
                    albumImage = await FetchImage(track.albumArt).ConfigureAwait(false);
                }
                if (track.title != trackTitle)
                {
                    trackTitle = track.title;
                    trackTitleShifted = new StringBuilder($"{trackTitle} - {track.artist}  ");
                }
            }

        }

        private void Client_TimeReceived(object sender, GPMDP_Api.Models.TimeValues e)
        {
            progress = e.Current;
        }

        private void DrawPlayPauseKey()
        {
            Bitmap img = Tools.GenerateGenericKeyImage(out Graphics graphics);
            int height = img.Height;
            int width = img.Width;

            GraphicsPath gpath;
            var fontSong = new Font("Verdana", 11, FontStyle.Bold);
            var fontElapsed = new Font("Verdana", 11, FontStyle.Bold);

            // Draw back cover
            if (Settings.ShowSongImage && track != null && albumImage != null)
            {
                graphics.DrawImage(albumImage, 0, 0, width, height);
            }
            else if (imgCustomizedPlay != null && gpmdpManager.IsPlaying)
            {
                graphics.DrawImage(imgCustomizedPlay, 0, 0, width, height);
            }
            else if (imgCustomizedPause != null && !gpmdpManager.IsPlaying)
            {
                graphics.DrawImage(imgCustomizedPause, 0, 0, width, height);
            }
            else // Default image
            {
                graphics.DrawImage(imgPlayPauseDefault, 0, 0, width, height);
            }

            if (Settings.ShowTimeElapsed)
            {
                // Draw Elapsed
                TimeSpan t = TimeSpan.FromMilliseconds(progress);

                string timeElapsed = string.Format("{0:D2}:{1:D2}",
                                        (int)t.TotalMinutes,
                                        t.Seconds);
                gpath = new GraphicsPath();
                gpath.AddString(timeElapsed,
                                    fontElapsed.FontFamily,
                                    (int)FontStyle.Bold,
                                    graphics.DpiY * fontElapsed.SizeInPoints / width,
                                    new Point(9, 54),
                                    new StringFormat());
                graphics.DrawPath(Pens.Black, gpath);
                graphics.FillPath(Brushes.White, gpath);
            }

            // Draw song name
            if (Settings.ShowSongName && trackTitleShifted != null && trackTitleShifted.Length > 3)
            {
                string songName = trackTitleShifted.ToString();
                string cutString = trackTitleShifted.ToString().Substring(0, 2);
                trackTitleShifted = trackTitleShifted.Remove(0, 2).Append(cutString);
                gpath = new GraphicsPath();
                gpath.AddString(songName,
                                fontSong.FontFamily,
                                (int)FontStyle.Bold,
                                graphics.DpiY * fontSong.SizeInPoints / width,
                                new Point(3, 1),
                                new StringFormat());
                graphics.DrawPath(Pens.Black, gpath);
                graphics.FillPath(Brushes.White, gpath);
            }
            Connection.SetImageAsync(img);
        }

        private async Task<Bitmap> FetchImage(string imageUrl)
        {
            if (String.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            WebClient client = new WebClient();
            Stream stream = await client.OpenReadTaskAsync(new Uri(imageUrl));
            return new Bitmap(stream);
        }

        private void LoadCustomImages()
        {
            // Play key
            if (!String.IsNullOrEmpty(Settings.PlayImage))
            {
                imgCustomizedPlay = Tools.Base64StringToImage(Tools.FileToBase64(Settings.PlayImage, true));
            }

            if (!string.IsNullOrEmpty(Settings.PauseImage))
            {
                imgCustomizedPause = Tools.Base64StringToImage(Tools.FileToBase64(Settings.PauseImage, true));
            }
        }

        #endregion

    }
}
