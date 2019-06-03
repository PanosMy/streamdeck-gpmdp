using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    public class PlaybackInfoEventArgs : EventArgs
    {
        public PlaybackInfo PlaybackInfo { get; private set; }

        public PlaybackInfoEventArgs(PlaybackInfo playbackInfo)
        {
            PlaybackInfo = playbackInfo;
        }
    }
}
