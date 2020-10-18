using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Controllers
{
    [Route("/web")]
    public class WebController : Controller
    {
        [Route("bancho_connect.php")]
        public async Task<IActionResult> BanchoConnect()
        {
            return Ok("<>");
        }


        /*
         * # append beatmap info chart (#1)
        charts.append(
            f'beatmapId:{s.bmap.id}|'
            f'beatmapSetId:{s.bmap.set_id}|'
            f'beatmapPlaycount:{s.bmap.plays}|'
            f'beatmapPasscount:{s.bmap.passes}|'
            f'approvedDate:{s.bmap.last_update}'
        )

        # append beatmap ranking chart (#2)
        charts.append('|'.join((
            'chartId:beatmap',
            f'chartUrl:https://akatsuki.pw/b/{s.bmap.id}',
            'chartName:Beatmap Ranking',

            ( # we had a score on the map prior to this
                f'rankBefore:{s.prev_best.rank}|rankAfter:{s.rank}|'
                f'rankedScoreBefore:{s.prev_best.score}|rankedScoreAfter:{s.score}|'
                f'totalScoreBefore:{s.prev_best.score}|totalScoreAfter:{s.score}|'
                f'maxComboBefore:{s.prev_best.max_combo}|maxComboAfter:{s.max_combo}|'
                f'accuracyBefore:{s.prev_best.acc:.2f}|accuracyAfter:{s.acc:.2f}|'
                f'ppBefore:{s.prev_best.pp:.4f}|ppAfter:{s.pp:.4f}|'
                f'onlineScoreId:{s.id}'
            ) if s.prev_best else ( # we don't, this is our first
                f'rankBefore:|rankAfter:{s.rank}|'
                f'rankedScoreBefore:|rankedScoreAfter:{s.score}|' # these are
                f'totalScoreBefore:|totalScoreAfter:{s.score}|' # prolly wrong
                f'maxComboBefore:|maxComboAfter:{s.max_combo}|'
                f'accuracyBefore:|accuracyAfter:{s.acc:.2f}|'
                f'ppBefore:|ppAfter:{s.pp:.4f}|'
                f'onlineScoreId:{s.id}'
            )
        )))#'|'.join(beatmap_chart))

        # append overall ranking chart (#3)
        charts.append('|'.join((
            'chartId:overall',
            f'chartUrl:https://akatsuki.pw/u/{s.player.id}',
            'chartName:Overall Ranking',

            # TODO: achievements
            ( # we have a score on the account prior to this
                f'rankBefore:{prev_stats.rank}|rankAfter:{stats.rank}|'
                f'rankedScoreBefore:{prev_stats.rscore}|rankedScoreAfter:{stats.rscore}|'
                f'totalScoreBefore:{prev_stats.tscore}|totalScoreAfter:{stats.tscore}|'
                f'maxComboBefore:{prev_stats.max_combo}|maxComboAfter:{stats.max_combo}|'
                f'accuracyBefore:{prev_stats.acc:.2f}|accuracyAfter:{stats.acc:.2f}|'
                f'ppBefore:{prev_stats.pp:.4f}|ppAfter:{stats.pp:.4f}|'
                # f'achievements-new:taiko-skill-pass-2+Katsu Katsu Katsu+Hora! Ikuzo!/taiko-skill-fc-2+To Your Own Beat+Straight and steady.|'
                f'onlineScoreId:{s.id}'
            ) if prev_stats else ( # this is the account's first score
                f'rankBefore:|rankAfter:{stats.rank}|'
                f'rankedScoreBefore:|rankedScoreAfter:{stats.rscore}|'
                f'totalScoreBefore:|totalScoreAfter:{stats.tscore}|'
                f'maxComboBefore:|maxComboAfter:{stats.max_combo}|'
                f'accuracyBefore:|accuracyAfter:{stats.acc:.2f}|'
                f'ppBefore:|ppAfter:{stats.pp:.4f}|'
                # f'achievements-new:taiko-skill-pass-2+Katsu Katsu Katsu+Hora! Ikuzo!/taiko-skill-fc-2+To Your Own Beat+Straight and steady.|'
                f'onlineScoreId:{s.id}'
            )

        )))
       */

        [Route("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular([FromBody] ScoreLayout layout)
        {   
            var score = layout.ToScore();
            return Ok("<>");
        }


        // TODO: make scores length configurable
        /*
         *     # Syntax
               # status|server_has_osz|bid|beatmap set id|len_scores
               # online_offset
               # map_name
               # map_rating: 1f
               # score_id|username|score|combo|n50|n100|n300|nmiss|nkatu|ngeki|perfect|mods|userid|rank|time|server_has_repla
         */
        [Route("osu-osz2-getscores.php")]
        public async Task<IActionResult> GetScores
        (
            [FromQuery(Name = "s")] bool getScores,
            [FromQuery(Name = "vv")] string scoreboardVersion, 
            [FromQuery(Name = "v")] RankingType scoreboardType,
            [FromQuery(Name = "c")] string beatmapChecksum, 
            [FromQuery(Name = "f")] string fileName,
            [FromQuery(Name = "m")] PlayMode mode,
            [FromQuery(Name = "i")] int setId,
            [FromQuery(Name = "mods")] Mods mods,
            [FromQuery(Name = "us")] string username, 
            [FromQuery(Name = "ha")] string password
        )
        {
            var context = new OyasumiDbContext();

            if (!(username, password).CheckLogin())
                return Ok("error: pass");

            //var artist = fileName.
            var (status, beatmap) = await BeatmapManager.Get(beatmapChecksum, new BeatmapTitle
            {
                Artist = fileName
            });

            Console.WriteLine(status.ToString());
            Console.WriteLine(beatmap.ToString());

            // TODO: add NeedUpdate
            return status switch
            {
                RankedStatus.NotSubmitted => Ok("-1|false"),
                RankedStatus.Approved => Ok(beatmap.ToString()),
                _ => Ok("error: no")
            };
        }
    }
}