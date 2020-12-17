using System;

namespace oyasumi.Enums
{
    [Flags]
    public enum BanchoPermissions
    {
        None = 0,
        Normal = 1,
        BAT = 2,
        Supporter = 4,
        Moderator = 8,
        Peppy = 16,
        Tournament = 32
    }
}