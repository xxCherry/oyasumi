using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public struct Chart
    {
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
        public override string ToString()
            => $"chartId:{_chartId}" +
               $"|chartUrl:{_chartUrl}" +
               $"|chartName:{_chartName}" +
               $"|rankBefore:{(_sBefore == null ? _pBefore.Rank : _sBefore.Rank)}" +
               $"|rankAfter:{(_sBefore == null ? _pAfter.Rank : _sAfter.Rank)}" +
               $"|maxComboBefore:0" +
               $"|maxComboAfter:0" +
               $"|accuracyBefore:{(_sBefore == null ? _pBefore.Accuracy : _sBefore.Accuracy)}" +
               $"|accuracyAfter:{(_sBefore == null ? _pAfter.Accuracy : _sAfter.Accuracy)}" +
               $"|rankedScoreBefore:{(_sBefore == null ? _pBefore.RankedScore : _sBefore.TotalScore)}" +
               $"|rankedScoreAfter:{(_sBefore == null ? _pAfter.RankedScore : _sAfter.TotalScore)}" +
               $"|totalScoreBefore:{(_sBefore == null ? _pBefore.TotalScore : _sBefore.TotalScore)}" +
               $"|totalScoreAfter:{(_sBefore == null ? _pAfter.TotalScore : _sAfter.TotalScore)}" +
               $"|ppBefore:{(_sBefore == null ? _pBefore.Performance : _sBefore.PerformancePoints)}" +
               $"|ppAfter:{(_sBefore == null ? _pAfter.Performance : _sAfter.PerformancePoints)}" +
               (_achievements == null ? "" : "|achievements-new:" + _achievements) +
               $"|onlineScoreId:{_scoreId}";
    }
}
