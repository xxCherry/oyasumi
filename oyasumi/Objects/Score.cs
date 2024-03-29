﻿using System;
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
using oyasumi.Managers;
using oyasumi.Utilities;
using DbContext = oyasumi.Database.DbContext;

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

        private static Comparison<DbScore> _scoreComparison = new((s1, s2) => s2.TotalScore.CompareTo(s1.TotalScore));
        private static Comparison<DbScore> _ppComparison = new ((s1, s2) => s2.PerformancePoints.CompareTo(s1.PerformancePoints));

        public static Score[] GetRawScores(string beatmapMd5, PlayMode mode, RankedStatus status,
            LeaderboardMode lbMode)
        {
            var scores = DbContext.Scores
                .Where(x => x.FileChecksum == beatmapMd5
                            && x.Completed == CompletedStatus.Best
                            && x.PlayMode == mode
                            && x.Relaxing == (lbMode == LeaderboardMode.Relax))
                .Take(50)
                .Join(DbContext.Users.Values, 
                    s => s.UserId,
                    u => u.Id, (score, user) => new
                    {
                        User = user,
                        Score = score
                    })
                .Where(x => !x.User.Banned())
                .Select(x => x.Score)
                .ToArray();

            if (status == RankedStatus.Loved || lbMode == LeaderboardMode.Vanilla)
                Array.Sort(scores, _scoreComparison);
            else
                Array.Sort(scores, _ppComparison);

            return scores.Select(score => FromDb(score, status, scores)).ToArray();
        }

        public static List<string> FormatScores(IEnumerable<Score> scores, RankedStatus status)
            => scores.Select(score => score.ToString(status)).ToList();
        
        public void CalculateLeaderboardRank(IReadOnlyList<Score> scores, RankedStatus status)
        {
            var orderedScores = scores.OrderByDescending
            (
                x => x.Relaxing && status != RankedStatus.Loved ? x.PerformancePoints : x.TotalScore
            ).ToArray();

            for (var i = 0; i < orderedScores.Length; i++)
            {
                if (orderedScores[i].UserId == UserId)
                {
                    Rank = i + 1;
                    break;
                }
            }
        }
        
        public void CalculateLeaderboardRank(IReadOnlyList<DbScore> scores, RankedStatus status)
        {
            var orderedScores = scores.OrderByDescending
            (
                x => x.Relaxing && status != RankedStatus.Loved ? x.PerformancePoints : x.TotalScore
            ).ToArray();

            for (var i = 0; i < orderedScores.Length; i++)
            {
                if (orderedScores[i].UserId == UserId)
                {
                    Rank = i + 1;
                    break;
                }
            }
        }

        public static Score FromDb(DbScore dbScore, RankedStatus status, DbScore[] scores = null)
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
            score.CalculateLeaderboardRank(scores, status);

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

        public string ToString(RankedStatus status) =>
             $"{ScoreId}|{User.Username}|{(Relaxing && status != RankedStatus.Loved ? (int)PerformancePoints : TotalScore)}|{MaxCombo}|{Count50}|{Count100}|{Count300}|{CountMiss}|{CountKatu}" +
             $"|{CountGeki}|{Perfect}|{(int)Mods}|{User.Id}|{Rank}|{Date.ToUnixTimestamp()}|{(string.IsNullOrEmpty(ReplayChecksum) ? "0" : "1")}";
    }
}
