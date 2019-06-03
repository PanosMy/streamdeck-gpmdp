using Newtonsoft.Json;
using System;

namespace BarRaider.GPMDP.Communication
{
    public class PlaybackInfo
    {
        [JsonProperty(PropertyName = "song")]
        public SongInfo Song { get; private set; }

        [JsonProperty(PropertyName = "playing")]
        public bool IsPlaying { get; private set; }

        [JsonProperty(PropertyName = "songLyrics")]
        public string Lyrics { get; private set; }

        [JsonProperty(PropertyName = "shuffle")]
        public string Shuffle { get; private set; }

        [JsonProperty(PropertyName = "repeat")]
        public string RepeatInt { get; private set; }

        [JsonProperty(PropertyName = "volume")]
        public int Volume { get; private set; }

        [JsonIgnore]
        public DateTime LastPlaybackRefresh { get; set; }
    }

    public class TimeInfo
    {
        [JsonProperty(PropertyName = "current")]
        public int CurrentTime { get; private set; }

        [JsonProperty(PropertyName = "total")]
        public int TotalTime { get; private set; }
    }
}