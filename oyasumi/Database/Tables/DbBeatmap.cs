using oyasumi.Database.Attributes;
using oyasumi.Enums;

namespace oyasumi.Database.Models
{
    [Table("Beatmaps")]
    public class DbBeatmap
    {
        public int BeatmapId { get; set; }
        public string BeatmapMd5 { get; set; }
        public int Id { get; set; }
        public string FileName { get; set; }
        public int BeatmapSetId { get; set; }
        public RankedStatus Status { get; set; }
        public bool Frozen { get; set; }
        public int PlayCount { get; set; }
        public int PassCount { get; set; } 
        public string Artist { get; set; }
        public string Title { get; set; }
        public string DifficultyName { get; set; }
        public string Creator { get; set; }
        public float BPM { get; set; }
        public float CircleSize { get; set; }
        public float OverallDifficulty { get; set; }
        public float ApproachRate { get; set; }
        public float HPDrainRate { get; set; }
        public float Stars { get; set; }
    }
}