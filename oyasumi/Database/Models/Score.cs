using oyasumi.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Database.Models
{
    [Table("Scores")]
    public class DbScore
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Count100 { get; set; }
        public int Count300 { get; set; }
        public int Count50 { get; set; }
        public int CountGeki { get; set; }
        public int CountKatu { get; set; }
        public int CountMiss { get; set; }
        public int TotalScore { get; set; }
        public double Accuracy { get; set; }
        [Column(TypeName = "varchar(32)")] public string FileChecksum { get; set; }
        public int MaxCombo { get; set; }
        public bool Passed { get; set; }
        public Mods Mods { get; set; }
        public PlayMode PlayMode { get; set; }
        public BadFlags Flags { get; set; }
        public int OsuVersion { get; set; }
        public bool Perfect { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        [Column(TypeName = "varchar(32)")] public string ReplayChecksum { get; set; }
        public bool Relaxing { get; set; }
        public bool AutoPiloting { get; set; }
        public double PerformancePoints { get; set; }
        public CompletedStatus Completed { get; set; }
    }
}
