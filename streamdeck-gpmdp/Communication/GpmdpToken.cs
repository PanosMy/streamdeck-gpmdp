using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    [Serializable]
    class GpmdpToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonIgnore]
        public DateTime TokenLastRefresh { get; set; }
    }
}
