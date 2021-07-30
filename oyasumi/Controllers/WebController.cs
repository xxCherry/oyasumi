using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Interfaces;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using SharpCompress.Compressors.LZMA;

namespace oyasumi.Controllers
{
    [Route("/web/")]
    public class WebController : OyasumiController
    {
        [Route("bancho_connect.php")]
        public IActionResult BanchoConnect()
        {
            return Ok("<>"); // force client to ready to connect (works as if response was empty)
            // return Ok("https://osu.ppy.sh");
        }

        [HttpGet("osu-search.php")]
        public async Task<IActionResult> DirectSearch
        (
            [FromQuery(Name = "u")] string username,
            [FromQuery(Name = "h")] string password,
            [FromQuery(Name = "p")] int page,
            [FromQuery(Name = "q")] string query,
            [FromQuery(Name = "m")] int mode,
            [FromQuery(Name = "r")] int status
        )
        {
            if (!(username, password).CheckLogin())
                return Ok("no");

            using var client = new HttpClient();

            var reqResult = await client.GetAsync(
                $"{Config.Properties.BeatmapMirror}/api/search?amount=100&offset={page}" +
                $"&query={query}{(mode != -1 ? $"&m={mode}" : "")}{(status != 4 ? $"&status={(int)Beatmap.DirectToApiRankedStatus[status]}" : "")}");

            if (!reqResult.IsSuccessStatusCode)
                return Ok("no");

            var beatmaps = JsonConvert.DeserializeObject<List<JsonBeatmap>>(await reqResult.Content.ReadAsStringAsync());

            var results = new List<string>();

            foreach (var beatmap in beatmaps)
            {
                var difficulties = new List<string>();

                if (beatmap.ChildrenBeatmaps is not null)
                {
                    foreach (var childBeatmap in beatmap.ChildrenBeatmaps)
                    {
                        difficulties.Add(
                            $"[{Math.Round(childBeatmap.DifficultyRating, 2).ToString().Replace(',', '.')}⭐] {childBeatmap.DiffName} " +
                            $"CS: {childBeatmap.CS.ToString().Replace(',', '.')}|" +
                            $"OD: {childBeatmap.OD.ToString().Replace(',', '.')}| " +
                            $"AR: {childBeatmap.AR.ToString().Replace(',', '.')}|" +
                            $"HP: {childBeatmap.HP.ToString().Replace(',', '.')}" +
                            $"@{childBeatmap.Mode}"); // any string can be used before '@'
                    }
                }

                results.Add($"{beatmap.SetID}.osz|{beatmap.Artist}|{beatmap.Title}|{beatmap.Creator}|{beatmap.RankedStatus}|10.0|{beatmap.LastUpdate}|{beatmap.SetID}" +
                            $"|0|0|0|0|0|{string.Join(",", difficulties)}");
            }

            var mapCount = beatmaps.Count + "\n";

            if (beatmaps.Count > 100)
                mapCount = "101\n";

            return Ok(mapCount + string.Join("\n", results));
        }

        [HttpGet("osu-search-set.php")]
        public IActionResult DirectSearchSet
        (
            [FromQuery(Name = "u")] string username,
            [FromQuery(Name = "h")] string password,
            [FromQuery(Name = "s")] int setId = 0,
            [FromQuery(Name = "b")] int beatmapId = 0
        )
        {
            if (!(username, password).CheckLogin())
                return Ok("no");

            DbBeatmap beatmap = null;

            if (Request.Query.ToList().Any(x => x.Key == "s"))
                beatmap = DbContext.Beatmaps.Values
                    .Where(x => x.BeatmapSetId == setId)
                    .FirstOrDefault();
            else if (Request.Query.ToList().Any(x => x.Key == "b"))
                beatmap = DbContext.Beatmaps.Values
                    .Where(x => x.BeatmapId == beatmapId)
                    .FirstOrDefault();

            if (beatmap is null)
                return Ok("no");

            // 0 after 10.0 (rating) is last updated
            return Ok(
                $"{beatmap.BeatmapSetId}.osz|{beatmap.Artist}|{beatmap.Title}|{beatmap.Creator}|{beatmap.Status}|10.0|0|{beatmap.BeatmapSetId}" +
                "|0|0|0|0|0");
        }

        [HttpPost("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular()
        {
            var score = await ((string)Request.Form["score"], (string)Request.Form["iv"],
                (string)Request.Form["osuver"]).ToScore();

            if (score is null)
                return Ok("error: no");
            if (score.Presence is null)
                return Ok("error: pass");
            if (!(score.Presence.Username, (string)Request.Form["pass"]).CheckLogin())
                return Ok("error: pass");
            if (score.User.Banned())
                return Ok("error: banned");

            var beatmap = score.Beatmap;

            var user = Base.UserCache[score.Presence.Username];

            if (user == default)
                return Ok("error: no");

            var lbMode = score.Mods switch
            {
                var mod when (mod & Mods.Relax) > 0 => LeaderboardMode.Relax,
                _ => LeaderboardMode.Vanilla
            };

            switch (beatmap.Status)
            {
                case RankedStatus.NotSubmitted:
                    return Ok("error: no");
                case RankedStatus.Loved:
                case RankedStatus.Approved:
                case RankedStatus.Qualified:
                case RankedStatus.Ranked:
                    var failed = Request.Form["x"] == "1";

                    var stats = Base.UserStatsCache[lbMode][score.Presence.Id];

                    if (failed || !score.Passed)
                    {
                        foreach (var otherPresence in PresenceManager.Presences.Values)
                        {
                            await score.Presence.UserStats(otherPresence);
                        }

                        var failTime = int.Parse(Request.Form["ft"]);

                        if (failTime > 1000)
                            score.Presence.AddPlaycount(stats, score.PlayMode);

                        score.Completed = CompletedStatus.Failed;
                        return Ok("error: no");
                    }

                    var presenceBefore = score.Presence;

                    score.Presence.UpdateAccuracy(stats, score.PlayMode, lbMode); // update old accuracy
                    score.Presence.UpdatePerformance(stats, score.PlayMode, lbMode); // update old performance

                    score.Presence.AddPlaycount(stats, score.PlayMode);

                    score.Presence.AddScore(stats, score.TotalScore, true, score.PlayMode);
                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);

                    score.Accuracy = (float)Calculator.CalculateAccuracy(score);

                    var oldDbScore = DbContext.Scores
                        .Where(x => x.Completed == CompletedStatus.Best &&
                                    x.UserId == score.Presence.Id &&
                                    x.FileChecksum == score.FileChecksum &&
                                    x.PlayMode == score.PlayMode &&
                                    x.Relaxing == (lbMode == LeaderboardMode.Relax))
                        .FirstOrDefault();

                    var replay = Request.Form.Files.GetFile("score");

                    await using (var m = new MemoryStream())
                    {
                        if (replay is not null)
                        {
                            await replay.CopyToAsync(m);
                            m.Position = 0;

                            score.ReplayChecksum = Crypto.ComputeHash(m.ToArray());

                            if (!string.IsNullOrEmpty(score.ReplayChecksum))
                            {
                                await System.IO.File.WriteAllBytesAsync($"data/osr/{score.ReplayChecksum}.osr",
                                    m.ToArray());
                            }
                        }
                    }

                    if (beatmap.Status != RankedStatus.Loved)
                        score.PerformancePoints = await Calculator.CalculatePerformancePoints(score);

                    score.Completed = CompletedStatus.Best;

                    if (oldDbScore is not null) // if we already have score on the beatmap
                    {
                        if (score.Relaxing && beatmap.Status != RankedStatus.Loved)
                        {
                            if (oldDbScore.PerformancePoints <= score.PerformancePoints) // then check if our last score is better.
                                oldDbScore.Completed = CompletedStatus.Submitted;
                            else
                                score.Completed = CompletedStatus.Submitted;
                        }
                        else
                        {
                            if (oldDbScore.TotalScore <= score.TotalScore) // then check if our last score is better.
                                oldDbScore.Completed = CompletedStatus.Submitted;
                            else
                                score.Completed = CompletedStatus.Submitted;
                        }
                    }

                    var dbScore = score.ToDb();
                    dbScore.Id = DbContext.Scores.Count + 1;

                    DbContext.Scores.Add(dbScore);

                    score.ScoreId = dbScore.Id;

                    score.Presence.UpdateAccuracy(stats, score.PlayMode, lbMode); // now get new accuracy
                    score.Presence.UpdatePerformance(stats, score.PlayMode, lbMode); // now get new performance

                    await score.Presence.UpdateRank(stats, score.PlayMode, lbMode);

                    IStats dbStats = lbMode switch
                    {
                        LeaderboardMode.Vanilla => DbContext.VanillaStats[stats.Id],
                        LeaderboardMode.Relax => DbContext.RelaxStats[stats.Id]
                    };

                    dbStats = stats;

                    score.Presence.GetOrUpdateUserStats(lbMode, true, stats);

                    var presenceAfter = score.Presence;

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        await score.Presence.UserStats(otherPresence);

                    var isLeaderboardModeCached = beatmap.LeaderboardCache.TryGetValue(lbMode, out var modeLeaderboard);

                    if (!isLeaderboardModeCached)
                        beatmap.InitializeLeaderboard(score.PlayMode);

                    var leaderboard = modeLeaderboard[score.PlayMode];

                    Score oldScore = null;
                    var oldScoreFound = false;

                    if (oldDbScore is not null)
                        oldScoreFound = leaderboard.TryGetValue(oldDbScore.UserId, out oldScore);

                    var scores = Score.GetRawScores(beatmap.FileChecksum, score.PlayMode, beatmap.Status, lbMode);

                    leaderboard.Clear(); // Clear the cache

                    foreach (var bScore in scores)
                    {
                        leaderboard.TryAdd(bScore.UserId, bScore);

                        if (bScore.Rank == 1 && bScore.UserId == score.UserId && oldScore?.Rank != 1)
                        {
                            await ChannelManager.SendMessage("oyasumi",
                                $"[{lbMode}] [https://astellia.club/{bScore.UserId} {presenceAfter.Username}] achieved #1 on https://osu.ppy.sh/b/{score.Beatmap.Id}",
                                "#announce", int.MaxValue, true);
                        }
                    }

                    score.CalculateLeaderboardRank(scores, beatmap.Status);

                    if (score.Rank == 1 && oldScore?.Rank != 1)
                    {
                        await ChannelManager.SendMessage("oyasumi",
                            $"[{lbMode}] [https://astellia.club/{score.UserId} {presenceAfter.Username}] " +
                            $"achieved #1 on [https://osu.ppy.sh/b/{score.Beatmap.Id} {score.Beatmap.Name}]",
                            "#announce", 1, true);
                    }

                    score.Beatmap.LeaderboardFormatted[lbMode][score.PlayMode] = Score.FormatScores(scores, beatmap.Status);

                    score.Presence.LastScore = score;

                    var bmChart = new Chart("beatmap", "astellia.club", "Beatmap Ranking", score.ScoreId, "",
                        !oldScoreFound ? score : oldScore, score, null, null);
                    var oaChart = new Chart("overall", "astellia.club", "Overall Ranking", score.ScoreId, "", null,
                        null, presenceBefore, presenceAfter);

                    return Ok(Chart.Build(beatmap, bmChart, oaChart));
                default: // map is unranked so we'll just add score and playcount
                    stats = lbMode switch
                    {
                        LeaderboardMode.Vanilla => DbContext.VanillaStats[score.Presence.Id],
                        LeaderboardMode.Relax => DbContext.RelaxStats[score.Presence.Id],
                        _ => throw new NotImplementedException(),
                    };

                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);
                    score.Presence.AddPlaycount(stats, score.PlayMode);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        await score.Presence.UserStats(otherPresence);

                    return Ok("error: no");
            }
        }

        [HttpGet("osu-getreplay.php")]
        public async Task<IActionResult> GetReplay
        (
            [FromQuery(Name = "c")] long scoreId,
            [FromQuery(Name = "m")] PlayMode mode,
            [FromQuery(Name = "u")] string username,
            [FromQuery(Name = "h")] string password
        )
        {
            if (!(username, password).CheckLogin())
                return Ok("error: pass");

            var replayChecksum = DbContext.Scores
                .Where(x => x.PlayMode == mode && x.Id == scoreId)
                .Select(x => x.ReplayChecksum)
                .FirstOrDefault();

            if (System.IO.File.Exists($"./data/osr/{replayChecksum}.osr"))
            {
                var file = System.IO.File.OpenRead($"./data/osr/{replayChecksum}.osr");
                return File(file, "binary/octet-stream");
            }

            return Ok("error: no-replay");

        }

        [HttpGet("osu-osz2-getscores.php")]
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
            if (scoreboardVersion != "4") // check on old client
                return Ok("error: pass");

            if (!(username, password).CheckLogin())
                return Ok("error: pass");

            var presence = PresenceManager.GetPresenceByName(username);

            var lbMode = mods switch
            {
                var mod when (mod & Mods.Relax) > 0 => LeaderboardMode.Relax,
                _ => LeaderboardMode.Vanilla,
            };

            switch (presence.Status.CurrentMods & Mods.Relax)
            {
                case 0 when lbMode == LeaderboardMode.Relax:
                    presence.Status.CurrentMods &= Mods.Relax;
                    break;
                case > 0 when lbMode == LeaderboardMode.Vanilla:
                    presence.Status.CurrentMods &= ~Mods.Relax;
                    break;
            }

            presence.GetOrUpdateUserStats(lbMode, false);
            await presence.UserStats();

            var beatmap = await BeatmapManager.Get(beatmapChecksum, setId: setId);

            if (beatmap is null)
                return Ok("-1|false");
            else if (beatmap.Status == RankedStatus.NeedUpdate)
                return Ok("1|false");
            else
            {
                var isLeaderboardModeCached = beatmap.LeaderboardCache.TryGetValue(lbMode, out var modeLeaderboard);

                if (!isLeaderboardModeCached)
                    beatmap.InitializeLeaderboard(mode);

                var personalBest = modeLeaderboard[mode].TryGetValue(Base.UserCache[username].Id, out var score);
                var personalBestString = string.Empty;

                if (personalBest)
                    personalBestString = score.ToString(beatmap.Status);

                var hasLeaderboard = beatmap.Status != RankedStatus.LatestPending;

                return Ok($"{beatmap.ToString(mode, lbMode)}\n"
                          + $"{(hasLeaderboard ? personalBestString : string.Empty)}\n"
                          + $"{(hasLeaderboard ? string.Join("\n", beatmap.LeaderboardFormatted[lbMode][mode]) : string.Empty)}");
            }
        }

        [HttpGet("maps/{fileName}")]
        public async Task<IActionResult> DownloadOsu(string fileName)
        {
            var beatmap = await BeatmapManager.Get(fileName: fileName);
            if (beatmap is not null)
            {
                var file = System.IO.File.OpenRead($"./data/beatmaps/{await Calculator.GetBeatmap(beatmap.FileChecksum)}.osu");
                return File(file, "octet-stream");
            }

            return NotFound();
        }

        [HttpGet("check-updates.php")]
        public async Task<IActionResult> CheckUpdates()
        {
            using var client = new HttpClient();

            var result = await client.GetAsync("https://old.ppy.sh/web/check-updates.php" + Request.QueryString.Value);

            if (!result.IsSuccessStatusCode)
                return NotFound();

            var content = await result.Content.ReadAsStringAsync();

            return Ok(content);
        }
    }
}