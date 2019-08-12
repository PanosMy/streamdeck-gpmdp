using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{    class GpmdpToken
    {
        [JsonProperty(PropertyName = "accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "lastRefresh")]
        public DateTime TokenLastRefresh { get; set; }
    }
}
