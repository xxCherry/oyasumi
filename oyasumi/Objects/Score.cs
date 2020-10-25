using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Utilities;

namespace oyasumi.Objects
{
    public class Score
    {
        public int ScoreId { get; set; }
        public Presence Presence { get; set; }
        public int Count100 { get; set; }
        public int Count300 { get; set; }
        public int Count50 { get; set; }
        public int CountGeki { get; set; }
        public int CountKatu { get; set; }
        public int CountMiss { get; set; }
        public int TotalScore { get; set; }
        public string FileChecksum { get; set; }
        public string ReplayChecksum { get; set; }
        public float PerformancePoints { get; set; }
        public float Accuracy { get; set; }
        public int MaxCombo { get; set; }
        public bool Passed { get; set; }
        public bool Perfect { get; set; }
        public bool Relaxing { get; set; }
        public bool Autopiloting { get; set; }
        public Mods Mods { get; set; }
        public PlayMode PlayMode { get; set; }
        public DateTime Date { get; set; }
        public Beatmap Beatmap { get; set; }
        public BadFlags Flags { get; set; }
        public int OsuVersion { get; set; }
        public int Rank { get; set; }

        public DbScore ToDb()
        {
            return new DbScore
            {
                Count50 = Count50,
                Count100 = Count100,
                Count300 = Count300,
                CountGeki = CountGeki,
                CountKatu = CountKatu,
                CountMiss = CountMiss,
                FileChecksum = FileChecksum,
                PerformancePoints = PerformancePoints,
                Accuracy = Accuracy,
                MaxCombo = MaxCombo,
                Passed = Passed,
                Relaxing = Relaxing,
                AutoPiloting = Autopiloting,
                Mods = Mods,
                PlayMode = PlayMode,
                UserId = Presence.Id,
                Date = Date,
                ReplayChecksum = ReplayChecksum,
                TotalScore = TotalScore,
                Flags = Flags,
                OsuVersion = OsuVersion
            };
        }


        public static async Task<List<Score>> GetRawScores(OyasumiDbContext context, string beatmapMd5)
        {
            var leaderboardData = new List<Score>();
            var scores = await context.Scores
                .AsQueryable()
                .Take(50)
                .ToListAsync();

            // omg, why i can't just use lambda func
            var scoreIds = scores
                .Where(x => x.FileChecksum == beatmapMd5) // get all scores on the beatmap by the beatmap's md5
                .OrderByDescending(x => x.TotalScore)
                .GroupBy(x => x.TotalScore)
                .Select((group, i) => new
                {
                    group.ToArray()[i].Id
                });

            foreach (var score in scoreIds)
                leaderboardData.Add(await FromDb(context, score.Id));


            return leaderboardData;
        }

        public static async Task<string> GetFormattedScores(OyasumiDbContext context, string beatmapMd5)
        {
            var sb = new StringBuilder();
            var scores = await GetRawScores(context, beatmapMd5);

            foreach (var score in scores)
                sb.AppendLine(score.ToString());

            return sb.ToString();
        }

        private async Task<int> GetLeaderboardRank(OyasumiDbContext context)
        {
            var scores = await context.Scores.AsQueryable().Take(50).ToListAsync();

            var urTuple = scores // u is user and r is rank
                .Where(x => x.FileChecksum == FileChecksum)
                .GroupBy(x => x.TotalScore)
                .Select((group, i) => new {
                    Rank = i + 1,
                    User = group.ToArray()[i]
                });

            foreach (var item in urTuple)
                if (item.User.UserId == Presence.Id) 
                    return item.Rank;

            return 0;
        }

        public static async Task<Score> FromDb(OyasumiDbContext context, int scoreId)
        {
            var dbScore = await context.Scores.AsAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == scoreId);

            var score = new Score
            {
                Presence = PresenceManager.GetPresenceById(dbScore.UserId),
                FileChecksum = dbScore.FileChecksum,
                TotalScore = dbScore.TotalScore,
                MaxCombo = dbScore.MaxCombo,
                Count50 = dbScore.Count50,
                Count100 = dbScore.Count100,
                Count300 = dbScore.Count300,
                CountMiss = dbScore.CountMiss,
                CountKatu = dbScore.CountKatu,
                CountGeki = dbScore.CountGeki,
                Perfect = dbScore.Perfect,
                Mods = dbScore.Mods
            };

            score.Rank = await score.GetLeaderboardRank(context);

            return score;
        }

        public override string ToString() =>
             $"{ScoreId}|{Presence.Username}|{TotalScore}|{MaxCombo}|{Count50}|{Count100}|{Count300}|{CountMiss}|{CountKatu}" +
             $"|{CountGeki}|{Perfect}|{(int)Mods}|{Presence.Id}|{Rank}|{Date.ToUnixTimestamp()}|{!string.IsNullOrEmpty(ReplayChecksum)}";
    }
}
