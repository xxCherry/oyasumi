using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Utilities;

namespace oyasumi.Objects
{
    public class Score
    {
        public int ScoreId { get; set; }
        public User User { get; set; }
        public Presence Presence { get; set; }
        public int UserId { get; set; }
        public int Count100 { get; set; }
        public int Count300 { get; set; }
        public int Count50 { get; set; }
        public int CountGeki { get; set; }
        public int CountKatu { get; set; }
        public int CountMiss { get; set; }
        public int TotalScore { get; set; }
        public string FileChecksum { get; set; }
        public string ReplayChecksum { get; set; }
        public double PerformancePoints { get; set; }
        public double Accuracy { get; set; }
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
        public CompletedStatus Completed { get; set; }

        public static async Task<List<Score>> GetRawScores(OyasumiDbContext context, string beatmapMd5, PlayMode mode,
            LeaderboardMode lbMode)
        {
            var leaderboardData = new List<Score>();

            var scores = (await context.Scores
                    .AsQueryable()
                    .ToArrayAsync())
                    .Where(x => x.FileChecksum == beatmapMd5 &&
                            x.Completed == CompletedStatus.Best &&
                            x.PlayMode == mode &&
                            x.Relaxing == (lbMode == LeaderboardMode.Relax) &&
                            !Base.UserCache[x.UserId].Banned())
                    .Take(50)
                    .ToArray();

            if (lbMode == LeaderboardMode.Vanilla)
                Array.Sort(scores, (s1, s2) => s2.TotalScore.CompareTo(s1.TotalScore));
            else
                Array.Sort(scores, (s1, s2) => s2.PerformancePoints.CompareTo(s1.PerformancePoints));

            foreach (var score in scores)
                leaderboardData.Add(await FromDb(context, score.Id, scores));

            return leaderboardData;
        }

        public static List<string> FormatScores(IEnumerable<Score> scores, PlayMode mode) => scores.Select(score => score.ToString()).ToList();

        public static async Task<string> GetFormattedScores(OyasumiDbContext context, string beatmapMd5, PlayMode mode, LeaderboardMode lbMode)
        {
            var sb = new StringBuilder();
            var scores = await GetRawScores(context, beatmapMd5, mode, lbMode);

            foreach (var score in scores)
                sb.AppendLine(score.ToString());

            return sb.ToString();
        }

        public int CalculateLeaderboardRank(IReadOnlyList<DbScore> scores)
        {
            for (var i = 0; i < scores.Count; i++)
                if (scores[i].UserId == User.Id)
                    return i + 1;
            return 0;
        }
        
        public void CalculateLeaderboardRank(IReadOnlyList<Score> scores)
        {
            for (var i = 0; i < scores.Count; i++)
                if (scores[i].UserId == User.Id)
                    Rank = i + 1;
        }

        public static async Task<Score> FromDb(OyasumiDbContext context, int scoreId, DbScore[] scores = null)
        {
            var dbScore = await context.Scores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == scoreId);

            var score = new Score
            {
                ScoreId = scoreId,
                User = Base.UserCache[dbScore.UserId],
                Date = dbScore.Date,
                UserId = dbScore.UserId,
                FileChecksum = dbScore.FileChecksum,
                ReplayChecksum = dbScore.ReplayChecksum,
                TotalScore = dbScore.TotalScore,
                PerformancePoints = dbScore.PerformancePoints,
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
            score.Relaxing = (score.Mods & Mods.Relax) != 0;
            score.Rank = score.CalculateLeaderboardRank(scores);

            return score;
        }

        public DbScore ToDb()
        {
            return new()
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
                UserId = User.Id,
                Date = Date,
                ReplayChecksum = ReplayChecksum,
                TotalScore = TotalScore,
                Flags = Flags,
                OsuVersion = OsuVersion,
                Completed = Completed
            };
        }

        public override string ToString() =>
             $"{ScoreId}|{User.Username}|{(Relaxing ? (int)PerformancePoints : TotalScore)}|{MaxCombo}|{Count50}|{Count100}|{Count300}|{CountMiss}|{CountKatu}" +
             $"|{CountGeki}|{Perfect}|{(int)Mods}|{User.Id}|{Rank}|{Date.ToUnixTimestamp()}|{(string.IsNullOrEmpty(ReplayChecksum) ? "0" : "1")}";
    }
}
