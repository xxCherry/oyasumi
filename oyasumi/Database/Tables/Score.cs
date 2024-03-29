﻿using oyasumi.Enums;
using oyasumi.Database.Attributes;
using System;

namespace oyasumi.Database.Models
{
    [Table("Scores")]
    public class DbScore
    {
        public int Id { get; set; }
        public string ReplayChecksum { get; set; }
        public string FileChecksum { get; set; }
        public int Count100 { get; set; }
        public int Count300 { get; set; }
        public int Count50 { get; set; }
        public int CountGeki { get; set; }
        public int CountKatu { get; set; }
        public int CountMiss { get; set; }
        public int TotalScore { get; set; }
        public double Accuracy { get; set; }
        public int MaxCombo { get; set; }
        public bool Passed { get; set; }
        public Mods Mods { get; set; }
        public PlayMode PlayMode { get; set; }
        public BadFlags Flags { get; set; }
        public int OsuVersion { get; set; }
        public bool Perfect { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public bool Relaxing { get; set; }
        public bool AutoPiloting { get; set; }
        public double PerformancePoints { get; set; }
        public CompletedStatus Completed { get; set; }
    }
}
