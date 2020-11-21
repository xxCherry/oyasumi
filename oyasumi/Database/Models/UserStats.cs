using oyasumi.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oyasumi.Database.Models
{
    [Table("VanillaStats")]
    public class VanillaStats : IStats
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long TotalScoreOsu { get; set; } = 0;
        public long TotalScoreTaiko { get; set; } = 0;
        public long TotalScoreCtb { get; set; } = 0;
        public long TotalScoreMania { get; set; } = 0;

        public long RankedScoreOsu { get; set; } = 0;
        public long RankedScoreTaiko { get; set; } = 0;
        public long RankedScoreCtb { get; set; } = 0;
        public long RankedScoreMania { get; set; } = 0;

        public int PerformanceOsu { get; set; } = 0;
        public int PerformanceTaiko { get; set; } = 0;
        public int PerformanceCtb { get; set; } = 0;
        public int PerformanceMania { get; set; } = 0;

        public float AccuracyOsu { get; set; } = 0;
        public float AccuracyTaiko { get; set; } = 0;
        public float AccuracyCtb { get; set; } = 0;
        public float AccuracyMania { get; set; } = 0;

        public int PlaycountOsu { get; set; } = 0;
        public int PlaycountTaiko { get; set; } = 0;
        public int PlaycountCtb { get; set; } = 0;
        public int PlaycountMania { get; set; } = 0;

        public int RankOsu { get; set; } = 0;
        public int RankTaiko { get; set; } = 0;
        public int RankCtb { get; set; } = 0;
        public int RankMania { get; set; } = 0;
    }

    [Table("RelaxStats")]
    public class RelaxStats : IStats
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long TotalScoreOsu { get; set; } = 0;
        public long TotalScoreTaiko { get; set; } = 0;
        public long TotalScoreCtb { get; set; } = 0;
        public long TotalScoreMania { get; set; } = 0;

        public long RankedScoreOsu { get; set; } = 0;
        public long RankedScoreTaiko { get; set; } = 0;
        public long RankedScoreCtb { get; set; } = 0;
        public long RankedScoreMania { get; set; } = 0;

        public int PerformanceOsu { get; set; } = 0;
        public int PerformanceTaiko { get; set; } = 0;
        public int PerformanceCtb { get; set; } = 0;
        public int PerformanceMania { get; set; } = 0;

        public float AccuracyOsu { get; set; } = 0;
        public float AccuracyTaiko { get; set; } = 0;
        public float AccuracyCtb { get; set; } = 0;
        public float AccuracyMania { get; set; } = 0;

        public int PlaycountOsu { get; set; } = 0;
        public int PlaycountTaiko { get; set; } = 0;
        public int PlaycountCtb { get; set; } = 0;
        public int PlaycountMania { get; set; } = 0;

        public int RankOsu { get; set; } = 0;
        public int RankTaiko { get; set; } = 0;
        public int RankCtb { get; set; } = 0;
        public int RankMania { get; set; } = 0;
    }
}