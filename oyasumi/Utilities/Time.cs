using System;

namespace oyasumi.Utilities
{
    public class Time
    {
        public static int CurrentUnixTimestamp => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
}