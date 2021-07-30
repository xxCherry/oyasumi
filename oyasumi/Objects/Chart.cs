using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public struct Chart
    {
        private static NumberFormatInfo _nfi = new CultureInfo("en-US", false).NumberFormat;

        private readonly string _chartId;
        private readonly string _chartUrl;
        private readonly string _chartName;

        private readonly Presence _pBefore;
        private readonly Presence _pAfter;

        private readonly Score _sBefore;
        private readonly Score _sAfter;

        private readonly string _achievements;

        private readonly int _scoreId;

        public Chart(string chartId, string chartUrl, string chartName, int scoreId, string achievements, Score sBefore, Score sAfter, Presence pBefore, Presence pAfter)
        {
            _chartId = chartId;
            _chartUrl = chartUrl;
            _chartName = chartName;
            _sBefore = sBefore;
            _sAfter = sAfter;
            _pBefore = pBefore;
            _pAfter = pAfter;
            _achievements = achievements;
            _scoreId = scoreId;
        }

        // TODO: max combo
        public override string ToString() =>
               $"chartId:{_chartId}" +
               $"|chartUrl:{_chartUrl}" +
               $"|chartName:{_chartName}" +
               $"|rankBefore:{_sBefore?.Rank ?? _pBefore.Rank}" +
               $"|rankAfter:{(_sBefore == null ? _pAfter.Rank : _sAfter.Rank)}" +
               $"|maxComboBefore:0" +
               $"|maxComboAfter:0" +
               $"|accuracyBefore:{_sBefore?.Accuracy * 100 ?? _pBefore.Accuracy * 100}" +
               $"|accuracyAfter:{(_sBefore == null ? _pAfter.Accuracy * 100 : _sAfter.Accuracy * 100)}" +
               $"|rankedScoreBefore:{_sBefore?.TotalScore ?? _pBefore.RankedScore}" +
               $"|rankedScoreAfter:{(_sBefore == null ? _pAfter.RankedScore : _sAfter.TotalScore)}" +
               $"|totalScoreBefore:{_sBefore?.TotalScore ?? _pBefore.TotalScore}" +
               $"|totalScoreAfter:{(_sBefore == null ? _pAfter.TotalScore : _sAfter.TotalScore)}" +
               $"|ppBefore:{_sBefore?.PerformancePoints ?? _pBefore.Performance}" +
               $"|ppAfter:{(_sBefore == null ? _pAfter.Performance : _sAfter.PerformancePoints)}" +
               (_achievements == null ? "" : "|achievements-new:" + _achievements) +
               $"|onlineScoreId:{_scoreId}";

        public static string Build(Beatmap beatmap, Chart beatmapChart, Chart overallChart) 
            => $"beatmapId:{beatmap.Id}|beatmapSetId:{beatmap.SetId}|beatmapPlaycount:0|beatmapPasscount:0|approvedDate:\n\n{beatmapChart}\n{overallChart}";
    }
}
