using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gulagDatabaseMerger.Database.Models
{
    [Table("scores_vn")]
    public class GulagScore
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] [Column("map_md5")] public string BeatmapChecksum { get; set; }
        [Required] [Column("userid")] public int UserId { get; set; }
        [Required] [Column("score")] public int Score { get; set; }
        [Required] [Column("status")] public int Completed { get; set; }
        [Required] [Column("max_combo")] public int MaxCombo { get; set; }
        [Required] [Column("mods")] public int Mods { get; set; }
        [Required] [Column("n300")] public int Count300 { get; set; }
        [Required] [Column("n100")] public int Count100 { get; set; }
        [Required] [Column("n50")] public int Count50 { get; set; }
        [Required] [Column("ngeki")] public int CountGeki { get; set; }
        [Required] [Column("nkatu")] public int CountKatu { get; set; }
        [Required] [Column("nmiss")] public int CountMiss { get; set; }
        [Required] [Column("play_time")] public string Time { get; set; } 
        [Required] [Column("mode")] public int PlayMode { get; set; }
        [Required] [Column("acc")] public double Accuracy { get; set; }
        [Required] [Column("pp")] public float Performance { get; set; }
    }
    
    [Table("scores_rx")]
    public class GulagRelaxScore
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] [Column("map_md5")] public string BeatmapChecksum { get; set; }
        [Required] [Column("userid")] public int UserId { get; set; }
        [Required] [Column("score")] public int Score { get; set; }
        [Required] [Column("status")] public int Completed { get; set; }
        [Required] [Column("max_combo")] public int MaxCombo { get; set; }
        [Required] [Column("mods")] public int Mods { get; set; }
        [Required] [Column("n300")] public int Count300 { get; set; }
        [Required] [Column("n100")] public int Count100 { get; set; }
        [Required] [Column("n50")] public int Count50 { get; set; }
        [Required] [Column("ngeki")] public int CountGeki { get; set; }
        [Required] [Column("nkatu")] public int CountKatu { get; set; }
        [Required] [Column("nmiss")] public int CountMiss { get; set; }
        [Required] [Column("play_type")] public string Time { get; set; }
        [Required] [Column("mode")] public int PlayMode { get; set; }
        [Required] [Column("acc")] public double Accuracy { get; set; }
        [Required] [Column("pp")] public float Performance { get; set; }
    }
}
