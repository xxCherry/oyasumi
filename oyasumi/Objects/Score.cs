using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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

        public static async Task<Score[]> GetRawScores(string beatmapMd5, PlayMode mode,
            LeaderboardMode lbMode)
        {
            IEnumerable<DbScore> scores = null;
            await using (var db = MySqlProvider.GetDbConnection())
            {
                scores = await db.QueryAsync<DbScore>($"SELECT * FROM Scores " +
                                                      $"JOIN Users ON Users.Id = Scores.UserId " +
                                                      $"WHERE Users.Privileges & {(int)Privileges.Normal} > 0 " +
                                                      $"AND FileChecksum = '{beatmapMd5}' " +
                                                      $"AND Completed = {(int)CompletedStatus.Best} " +
                                                      $"AND PlayMode = {(int)mode} " +
                                                      $"AND Relaxing = {lbMode == LeaderboardMode.Relax} " +
                                                      $"ORDER BY {(lbMode == LeaderboardMode.Relax ? "PerformancePoints" : "TotalScore")} " +
                                                      $"LIMIT 50");
            }
            var dbScores = scores as DbScore[] ?? scores.ToArray();
            
            return dbScores.Select(score => FromDb(score, dbScores)).ToArray();
        }

        public static List<string> FormatScores(IEnumerable<Score> scores, PlayMode mode) => scores.Select(score => score.ToString()).ToList();

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

        public static Score FromDb(DbScore dbScore, DbScore[] scores = null)
        {
            var score = new Score
            {
                ScoreId = dbScore.Id,
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
