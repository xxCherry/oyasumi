using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Enums
{
    public enum Privileges
    {
        Banned = 1 << 0,
        Restricted = 1 << 1,
        Normal = 2 << 1,
        Verified = 3 << 1
    }
}
