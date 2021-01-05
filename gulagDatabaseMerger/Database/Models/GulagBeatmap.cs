using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using oyasumi.Enums;

namespace gulagDatabaseMerger.Database.Models
{

    [Table("maps")]
    public class GulagBeatmap
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] [Column("md5")] public string Checksum { get; set; }
        [Required] [Column("status")] public RankedStatus Status { get; set; }
        [Required] [Column("set_id")] public int SetId { get; set; }
        [Required] [Column("artist")] public string Artist { get; set; }
        [Required] [Column("title")] public string Title { get; set; }
        [Required] [Column("version")] public string Version { get; set; }
        [Required] [Column("creator")] public string Creator { get; set; }
        [Required] [Column("plays")] public int PlayCount { get; set; }
        [Required] [Column("passes")] public int PassCount { get; set; }
        [Required] [Column("frozen")] public bool Frozen { get; set; }
        [Required] [Column("bpm")] public float BPM { get; set; }
        [Required] [Column("cs")] public float CS { get; set; }
        [Required] [Column("ar")] public float AR { get; set; }
        [Required] [Column("od")] public float OD { get; set; }
        [Required] [Column("hp")] public float HP { get; set; }
        [Required] [Column("diff")] public float SR { get; set; }
    }
}