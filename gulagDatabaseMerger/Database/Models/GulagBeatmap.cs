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
    }
}