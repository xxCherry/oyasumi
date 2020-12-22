using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using oyasumi.Enums;

namespace RippleDatabaseMerger.Database.Models
{

    [Table("beatmaps")]
    public class RippleBeatmap
    {
        [Required]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] [Column("beatmap_md5")] public string Checksum { get; set; }

        [Required] [Column("ranked")] public RankedStatus Status { get; set; }
    }
}