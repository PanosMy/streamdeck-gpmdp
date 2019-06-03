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
        private int progress;
        private string lastAlbumArtUrl = string.Empty;
        private Bitmap albumImage = null;
        private Image imgPlayPause = Tools.Base64StringToImage(Properties.Settings.Default.ImgPlayPause);
        private string trackTitle = null;
        private StringBuilder trackTitleShifted = null;

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

        private void Client_PlayStateReceived(object sender, bool e)
        {
            isPlaying = e;
        }

        private void DrawPlayPauseKey()
        {
            Graphics graphics;
            Bitmap bmp = Tools.GenerateKeyImage(out graphics);
            GraphicsPath gpath;
            var fontSong = new Font("Verdana", 11, FontStyle.Bold);
            var fontElapsed = new Font("Verdana", 11, FontStyle.Bold);

            // Draw back cover
            if (Settings.ShowSongImage && track != null && albumImage != null)
            {
                graphics.DrawImage(albumImage, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);
            }
            /*
            else if (keyBase64ImageStr != null)
            {
                if (imgKeyImage == null)
                {
                    imgKeyImage = Tools.Base64StringToImage(keyBase64ImageStr);
                }
                graphics.DrawImage(imgKeyImage, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);
            }*/
            else // Default image
            {
                graphics.DrawImage(imgPlayPause, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);
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
                                    graphics.DpiY * fontElapsed.SizeInPoints / 72,
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
                                graphics.DpiY * fontSong.SizeInPoints / 72,
                                new Point(3, 1),
                                new StringFormat());
                graphics.DrawPath(Pens.Black, gpath);
                graphics.FillPath(Brushes.White, gpath);
            }
            Connection.SetImageAsync(bmp);
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


        #endregion

    }
}
