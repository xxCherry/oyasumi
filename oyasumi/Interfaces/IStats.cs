using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Interfaces
{
    public interface IStats
    {
        public int Id { get; set; }
        public bool IsPublic { get; set; }
        public long TotalScoreOsu { get; set; }
        public long TotalScoreTaiko { get; set; }
        public long TotalScoreCtb { get; set; }
        public long TotalScoreMania { get; set; }

        public long RankedScoreOsu { get; set; }
        public long RankedScoreTaiko { get; set; }
        public long RankedScoreCtb { get; set; }
        public long RankedScoreMania { get; set; }

        public int PerformanceOsu { get; set; }
        public int PerformanceTaiko { get; set; }
        public int PerformanceCtb { get; set; }
        public int PerformanceMania { get; set; }

        public float AccuracyOsu { get; set; }
        public float AccuracyTaiko { get; set; }
        public float AccuracyCtb { get; set; }
        public float AccuracyMania { get; set; }

        public int PlaycountOsu { get; set; }
        public int PlaycountTaiko { get; set; }
        public int PlaycountCtb { get; set; }
        public int PlaycountMania { get; set; }

        public int RankOsu { get; set; }
        public int RankTaiko { get; set; }
        public int RankCtb { get; set; }
        public int RankMania { get; set; }
    }
}
