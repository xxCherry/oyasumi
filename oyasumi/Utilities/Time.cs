using System;

namespace oyasumi.Utilities
{
    public static class Time
    {
        public static int ToUnixTimestamp(this DateTime self) =>
            (int) self.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        public static int CurrentUnixTimestamp 
            => (int)DateTimeOffset.Now.ToUnixTimeSeconds();

        public static DateTime UnixTimestampFromDateTime(int timestamp) =>
            new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(timestamp)
                .ToLocalTime();
    }
}