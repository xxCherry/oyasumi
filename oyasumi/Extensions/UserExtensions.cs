using oyasumi.Managers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Interfaces;

namespace oyasumi.Extensions
{
    public static class UserExtensions
    {
        public static async Task<(string username, string password, int timezone)> ParseLoginDataAsync(this Stream self)
        {
            using var reader = new StreamReader(self, leaveOpen: true);

            var username = await reader.ReadLineAsync();
            var password = await reader.ReadLineAsync();
            var data = (await reader.ReadLineAsync()).Split("|");

            return (username, password, int.Parse(data[1]));
        }

        public static bool CheckLogin(this (string username, string password) self)
        {
            var pr = PresenceManager.GetPresenceByName(self.username);
            return pr is not null && Base.PasswordCache.TryGetValue(self.password, out _);
        }

        public static string ToSafe(this string self) =>
            self.Replace(" ", "_").ToLower();

        public static bool Banned(this User self) => 
            (self.Privileges & Privileges.Normal) == 0;

        public static long TotalScore(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.TotalScoreOsu,
                PlayMode.Taiko => stats.TotalScoreTaiko,
                PlayMode.CatchTheBeat => stats.TotalScoreCtb,
                PlayMode.OsuMania => stats.TotalScoreMania
            };
        }
        
        public static long RankedScore(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.RankedScoreOsu,
                PlayMode.Taiko => stats.RankedScoreTaiko,
                PlayMode.CatchTheBeat => stats.RankedScoreCtb,
                PlayMode.OsuMania => stats.RankedScoreMania
            };
        }
        
        public static int Performance(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.PerformanceOsu,
                PlayMode.Taiko => stats.PerformanceTaiko,
                PlayMode.CatchTheBeat => stats.PerformanceCtb,
                PlayMode.OsuMania => stats.PerformanceMania
            };
        }
        
        public static float Accuracy(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.AccuracyOsu,
                PlayMode.Taiko => stats.AccuracyTaiko,
                PlayMode.CatchTheBeat => stats.AccuracyCtb,
                PlayMode.OsuMania => stats.AccuracyMania
            };
        }
        
        public static int Playcount(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.PlaycountOsu,
                PlayMode.Taiko => stats.PlaycountTaiko,
                PlayMode.CatchTheBeat => stats.PlaycountCtb,
                PlayMode.OsuMania => stats.PlaycountMania
            };
        }
        
        public static int Rank(this IStats stats, PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Osu => stats.RankOsu,
                PlayMode.Taiko => stats.RankTaiko,
                PlayMode.CatchTheBeat => stats.RankCtb,
                PlayMode.OsuMania => stats.RankMania
            };
        }
    }
}