using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RippleDatabaseMerger.Database.Models
{
    [Table("scores")]
    public class RippleScore
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] [Column("beatmap_md5")] public string BeatmapChecksum { get; set; }
        [Required] [Column("userid")] public int UserId { get; set; }
        [Required] [Column("score")] public int Score { get; set; }
        [Required] [Column("max_combo")] public int MaxCombo { get; set; }
        [Required] [Column("mods")] public int Mods { get; set; }
        [Required] [Column("300_count")] public int Count300 { get; set; }
        [Required] [Column("100_count")] public int Count100 { get; set; }
        [Required] [Column("50_count")] public int Count50 { get; set; }
        [Required] [Column("gekis_count")] public int CountGeki { get; set; }
        [Required] [Column("katus_count")] public int CountKatu { get; set; }
        [Required] [Column("misses_count")] public int CountMiss { get; set; }
        [Required] [Column("time")] public string Time { get; set; } 
        [Required] [Column("play_mode")] public sbyte PlayMode { get; set; }
        [Required] [Column("accuracy")] public double Accuracy { get; set; }
        [Required] [Column("pp")] public float Performance { get; set; }
        [Required] [Column("completed")] public byte Completed { get; set; }
    }
    
    [Table("scores_relax")]
    public class RippleRelaxScore
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] [Column("beatmap_md5")] public string BeatmapChecksum { get; set; }
        [Required] [Column("userid")] public int UserId { get; set; }
        [Required] [Column("score")] public int Score { get; set; }
        [Required] [Column("max_combo")] public int MaxCombo { get; set; }
        [Required] [Column("mods")] public int Mods { get; set; }
        [Required] [Column("300_count")] public int Count300 { get; set; }
        [Required] [Column("100_count")] public int Count100 { get; set; }
        [Required] [Column("50_count")] public int Count50 { get; set; }
        [Required] [Column("gekis_count")] public int CountGeki { get; set; }
        [Required] [Column("katus_count")] public int CountKatu { get; set; }
        [Required] [Column("misses_count")] public int CountMiss { get; set; }
        [Required] [Column("time")] public string Time { get; set; } 
        [Required] [Column("play_mode")] public sbyte PlayMode { get; set; }
        [Required] [Column("accuracy")] public double Accuracy { get; set; }
        [Required] [Column("pp")] public float Performance { get; set; }
        [Required] [Column("completed")] public byte Completed { get; set; }
    }
}
