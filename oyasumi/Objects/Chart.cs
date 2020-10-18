using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public class Chart
    {
        private readonly string _chartId;
        private readonly string _chartUrl;
        private readonly string _chartName;

        private readonly Presence _before;
        private readonly Presence _after;

        private readonly string _achievements;

        private readonly int _scoreId;

        public Chart(string chartId, string chartUrl, string chartName, int scoreId, string achievements, Presence before, Presence after)
        {
            _chartId = chartId;
            _chartUrl = chartUrl;
            _chartName = chartName;
            _before = before;
            _after = after;
            _achievements = achievements;
            _scoreId = scoreId;
        }

        // TODO: max combo
        public override string ToString()
            => $"chartId:{_chartId}" +
               $"|chartUrl:{_chartUrl}" +
               $"|chartName:{_chartName}" +
               $"|rankBefore:{_before.Rank}" +
               $"|rankAfter:{_after.Rank}" +
               $"|maxComboBefore:0" +
               $"|maxComboAfter:0" +
               $"|accuracyBefore:{_before.Accuracy}" +
               $"|accuracyAfter:{_after.Accuracy}" +
               $"|rankedScoreBefore:{_before.RankedScore}" +
               $"|rankedScoreAfter:{_after.RankedScore}" +
               $"|totalScoreBefore:{_before.TotalScore}" +
               $"|totalScoreAfter:{_after.TotalScore}" +
               $"|ppBefore:{_before.Performance}" +
               $"|ppAfter:{_after.Performance}" +
               (_achievements == null ? "" : "|achievements-new:" + _achievements) +
               $"|onlineScoreId:{_scoreId}";
    }
}
