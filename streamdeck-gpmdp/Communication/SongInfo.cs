using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    public class SongInfo
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; private set; }

        [JsonProperty(PropertyName = "artist")]
        public string Artist { get; private set; }

        [JsonProperty(PropertyName = "album")]
        public string Album { get; private set; }

        [JsonProperty(PropertyName = "albumArt")]
        public string AlbumArt { get; private set; }



    }
}
