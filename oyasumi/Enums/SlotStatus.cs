using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Enums
{
    [Flags]
    public enum SlotStatus
    {
        Open = 1,
        Locked = 2,
        NotReady = 4,
        Ready = 8,
        NoMap = 16,
        Playing = 32,
        Complete = 64,
        HasPlayer = NotReady | Ready | NoMap | Playing | Complete,
        Quit = 128
    }
}
