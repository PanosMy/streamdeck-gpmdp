using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    class GlobalSettings
    {
        [JsonProperty(PropertyName = "token")]
        public GpmdpToken Token { get; set; }
    }
}
