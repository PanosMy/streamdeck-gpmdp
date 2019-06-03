using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.GPMDP.Communication
{
    public class GpmdpTokenEventArgs : EventArgs
    {
        public bool TokenExists { get; private set; }

        public GpmdpTokenEventArgs(bool tokenExists)
        {
            TokenExists = tokenExists;
        }
    }
}
