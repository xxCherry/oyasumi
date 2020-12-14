using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Enums
{
    [Flags]
    public enum Privileges
    {
        Banned = 1 << 0,
        Restricted = 1 << 1,
        Normal = 1 << 2,
        Verified = 1 << 3,
        ManageBeatmaps = 1 << 4,
        ManageUsers = 1 << 5
    }
}
