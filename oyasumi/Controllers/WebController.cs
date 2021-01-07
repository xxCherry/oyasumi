using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using ManagedBass.Fx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using NUnit.Framework;
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
    public class WebController : Controller
    {
        private readonly OyasumiDbContext _context;

        public WebController(OyasumiDbContext context) =>
            _context = context;

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
            [FromQuery(Name = "m")] int mods,
            [FromQuery(Name = "r")] int status
        )
        {
            if (!(username, password).CheckLogin())
                return Ok("no");

            using var client = new HttpClient();

            var reqResult = await client.GetAsync(
                $"{Config.Properties.BeatmapMirror}/api/search?amount=100&offset={page}" +
                $"&query={query}{(mods != -1 ? $"&m={mods}" : "")}{(status != 4 ? $"&status={(int) Beatmap.DirectToApiRankedStatus[status]}" : "")}");

            if (!reqResult.IsSuccessStatusCode)
                return Ok("no");

            var beatmaps =
                JsonConvert.DeserializeObject<List<JsonBeatmap>>(await reqResult.Content.ReadAsStringAsync());

            var results = new List<string>();

            foreach (var beatmap in beatmaps)
            {
                var difficulties = new List<string>();

                if (beatmap.ChildrenBeatmaps is not null)
                {
                    foreach (var childBeatmap in beatmap.ChildrenBeatmaps)
                        difficulties.Add(
                            $"[{Math.Round(childBeatmap.DifficultyRating, 2).ToString().Replace(',', '.')}⭐] {childBeatmap.DiffName} " +
                            $"CS: {childBeatmap.CS.ToString().Replace(',', '.')}|" +
                            $"OD: {childBeatmap.OD.ToString().Replace(',', '.')}| " +
                            $"AR: {childBeatmap.AR.ToString().Replace(',', '.')}|" +
                            $"HP: {childBeatmap.HP.ToString().Replace(',', '.')}" +
                            $"@{childBeatmap.Mode}"); // any string can be used before '@'
                }

                results.Add(
                    $"{beatmap.SetID}.osz|{beatmap.Artist}|{beatmap.Title}|{beatmap.Creator}|{beatmap.RankedStatus}|10.0|{beatmap.LastUpdate}|{beatmap.SetID}" +
                    $"|0|0|0|0|0|{string.Join(",", difficulties)}");
            }

            var mapCount = beatmaps.Count + "\n";

            if (beatmaps.Count > 100)
                mapCount = "101\n";

            return Ok(mapCount + string.Join("\n", results).Replace("f6b", ""));
        }

        [HttpGet("osu-search-set.php")]
        public async Task<IActionResult> DirectSearchSet
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
                beatmap = await _context.Beatmaps.AsNoTracking().AsAsyncEnumerable().Where(x => x.BeatmapSetId == setId)
                    .FirstOrDefaultAsync();
            else if (Request.Query.ToList().Any(x => x.Key == "b"))
                beatmap = await _context.Beatmaps.AsNoTracking().AsAsyncEnumerable()
                    .Where(x => x.BeatmapId == beatmapId)
                    .FirstOrDefaultAsync();

            if (beatmap is null)
                return Ok("no");

            // 0 after 10.0 (rating) is last updated
            return Ok(
                $"{beatmap.BeatmapSetId}.osz|{beatmap.Artist}|{beatmap.Title}|{beatmap.Creator}|{beatmap.Status}|10.0|0|{beatmap.BeatmapSetId}" +
                $"|0|0|0|0|0");
        }

        [HttpPost("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular()
        {
            var score = await ((string) Request.Form["score"], (string) Request.Form["iv"],
                (string) Request.Form["osuver"]).ToScore();

            if (score is null)
                return Ok("error: no");
            if (score.Presence is null)
                return Ok("error: pass");
            if (!(score.Presence.Username, (string) Request.Form["pass"]).CheckLogin())
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

                    IStats stats = lbMode switch
                    {
                        LeaderboardMode.Vanilla => await _context.VanillaStats.AsQueryable()
                            .FirstOrDefaultAsync(x => x.Id == score.Presence.Id),
                        LeaderboardMode.Relax => await _context.RelaxStats.AsQueryable()
                            .FirstOrDefaultAsync(x => x.Id == score.Presence.Id),
                    };

                    if (failed || !score.Passed)
                    {
                        foreach (var otherPresence in PresenceManager.Presences.Values)
                            await score.Presence.UserStats(otherPresence);

                        var failTime = int.Parse(Request.Form["ft"]);

                        if (failTime > 10000) // to reduce playcount abusers, we'll just set check if failTime > 10sec
                            score.Presence.AddPlaycount(stats, score.PlayMode);

                        score.Completed = CompletedStatus.Failed;
                        return Ok("error: no");
                    }

                    var presenceBefore = score.Presence;

                    await score.Presence.UpdateAccuracy(_context, stats, score.PlayMode, lbMode); // update old accuracy
                    await score.Presence.UpdatePerformance(_context, stats, score.PlayMode,
                        lbMode); // update old performance

                    score.Presence.AddPlaycount(stats, score.PlayMode);

                    score.Presence.AddScore(stats, score.TotalScore, true, score.PlayMode);
                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);

                    score.Accuracy = (float) Calculator.CalculateAccuracy(score);

                    var oldDbScore = await _context.Scores
                        .AsAsyncEnumerable()
                        .Where(x => x.Completed == CompletedStatus.Best &&
                                    x.UserId == score.Presence.Id &&
                                    x.FileChecksum == score.FileChecksum &&
                                    x.PlayMode == score.PlayMode &&
                                    x.Relaxing == (lbMode == LeaderboardMode.Relax))
                        .FirstOrDefaultAsync();

                    var replay = Request.Form.Files.GetFile("score");

                    await using (var m = new MemoryStream())
                    {
                        if (replay is not null)
                        {
                            await replay.CopyToAsync(m);
                            m.Position = 0;

                            score.ReplayChecksum = Crypto.ComputeHash(m.ToArray());

                            if (!string.IsNullOrEmpty(score.ReplayChecksum))
                                await System.IO.File.WriteAllBytesAsync($"data/osr/{score.ReplayChecksum}.osr",
                                    m.ToArray());
                        }
                    }

                    if (beatmap.Status != RankedStatus.Loved)
                        score.PerformancePoints = await Calculator.CalculatePerformancePoints(score);

                    score.Completed = CompletedStatus.Best;

                    if (oldDbScore is not null) // if we already have score on the beatmap
                    {
                        if (score.Relaxing && beatmap.Status != RankedStatus.Loved)
                        {
                            if (oldDbScore.PerformancePoints <= score.PerformancePoints
                            ) // then check if our last score is better.
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

                    // TODO: maybe there's another way to do it, like just pass object to param?
                    await using (var db = MySqlProvider.GetDbConnection())
                    {
                        var scoreId = await db.ExecuteAsync(
                            "INSERT INTO Scores " +
                            "(" +
                            "Count300, Count100, Count50, CountGeki, " +
                            "CountKatu, CountMiss, TotalScore, Accuracy, FileChecksum, " +
                            "MaxCombo, Passed, Mods, PlayMode, Flags, OsuVersion, Perfect, " +
                            "UserId, Date, ReplayChecksum, Relaxing, AutoPiloting, " +
                            "PerformancePoints, Completed" +
                            ") " +
                            "VALUES " +
                            "(" +
                            "@Count300, @Count100, @Count50, @CountGeki, " +
                            "@CountKatu, @CountMiss, @TotalScore, @Accuracy, @FileChecksum, " +
                            "@MaxCombo, @Passed, @Mods, @PlayMode, @Flags, @OsuVersion, @Perfect, " +
                            "@UserId, @Date, @ReplayChecksum, @Relaxing, @AutoPiloting, " +
                            "@PerformancePoints, @Completed" +
                            ");"
                            , new
                            {
                                dbScore.Count100,
                                dbScore.Count300,
                                dbScore.Count50,
                                dbScore.CountGeki,
                                dbScore.CountKatu,
                                dbScore.CountMiss,
                                dbScore.TotalScore,
                                dbScore.Accuracy,
                                dbScore.FileChecksum,
                                dbScore.MaxCombo,
                                dbScore.Passed,
                                dbScore.Mods,
                                dbScore.PlayMode,
                                dbScore.Flags,
                                dbScore.OsuVersion,
                                dbScore.Perfect,
                                dbScore.UserId,
                                dbScore.Date,
                                dbScore.ReplayChecksum,
                                dbScore.Relaxing,
                                dbScore.AutoPiloting,
                                dbScore.PerformancePoints,
                                dbScore.Completed
                            });

                        score.ScoreId = scoreId;
                    }

                    await score.Presence.UpdateAccuracy(_context, stats, score.PlayMode,
                        lbMode); // now get new accuracy
                    await score.Presence.UpdatePerformance(_context, stats, score.PlayMode,
                        lbMode); // now get new performance

                    await _context.SaveChangesAsync();

                    await score.Presence.UpdateRank(_context, stats, score.PlayMode, lbMode);

                    await score.Presence.GetOrUpdateUserStats(_context, lbMode, true);

                    var presenceAfter = score.Presence;

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        await score.Presence.UserStats(otherPresence);

                    var isLeaderboardModeCached = beatmap.LeaderboardCache.TryGetValue(lbMode, out var modeLeaderboard);

                    if (!isLeaderboardModeCached)
                        await beatmap.InitializeLeaderboard(score.PlayMode);

                    var leaderboard = modeLeaderboard[score.PlayMode];

                    Score oldScore = null;
                    var oldScoreFound = false;

                    if (oldDbScore is not null)
                        oldScoreFound = leaderboard.TryGetValue(oldDbScore.UserId, out oldScore);

                    var scores = await Score.GetRawScores(beatmap.FileChecksum, score.PlayMode, beatmap.Status, lbMode);

                    leaderboard.Clear(); // Clear the cache

                    foreach (var bScore in scores)
                    {
                        leaderboard.TryAdd(bScore.UserId, bScore);

                        if (bScore.Rank == 1 && bScore.UserId == score.UserId && oldScore?.Rank != 1)
                            await ChannelManager.SendMessage("oyasumi",
                                $"[{lbMode}] [https://astellia.club/{bScore.UserId} {presenceAfter.Username}] achieved #1 on https://osu.ppy.sh/b/{score.Beatmap.Id}",
                                "#announce", int.MaxValue, true);
                    }

                    score.CalculateLeaderboardRank(scores, beatmap.Status);

                    if (score.Rank == 1 && oldScore?.Rank != 1)
                        await ChannelManager.SendMessage("oyasumi",
                            $"[{lbMode}] [https://astellia.club/{score.UserId} {presenceAfter.Username}] " +
                            $"achieved #1 on [https://osu.ppy.sh/b/{score.Beatmap.Id} {score.Beatmap.BeatmapName}]",
                            "#announce", 1, true);

                    score.Beatmap.LeaderboardFormatted[lbMode][score.PlayMode] =
                        Score.FormatScores(scores, beatmap.Status, score.PlayMode);

                    score.Presence.LastScore = score;

                    var bmChart = new Chart("beatmap", "astellia.club", "Beatmap Ranking", score.ScoreId, "",
                        !oldScoreFound ? score : oldScore, score, null, null);
                    var oaChart = new Chart("overall", "astellia.club", "Overall Ranking", score.ScoreId, "", null,
                        null, presenceBefore, presenceAfter);

                    return Ok(
                        $"beatmapId:{beatmap.Id}|beatmapSetId:{beatmap.SetId}|beatmapPlaycount:0|beatmapPasscount:0|approvedDate:\n\n" +
                        bmChart
                        + "\n"
                        + oaChart);
                default: // map is unranked so we'll just add score and playcount
                    stats = lbMode switch
                    {
                        LeaderboardMode.Vanilla => await _context.VanillaStats.AsAsyncEnumerable()
                            .FirstOrDefaultAsync(x => x.Id == score.Presence.Id),
                        LeaderboardMode.Relax => await _context.RelaxStats.AsAsyncEnumerable()
                            .FirstOrDefaultAsync(x => x.Id == score.Presence.Id),
                    };

                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);
                    score.Presence.AddPlaycount(stats, score.PlayMode);
                    await score.Presence.Apply(_context);

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

            var replayChecksum = await _context.Scores
                .AsAsyncEnumerable()
                .Where(x => x.PlayMode == mode && x.Id == scoreId)
                .Select(x => x.ReplayChecksum)
                .FirstOrDefaultAsync();

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

            await presence.GetOrUpdateUserStats(_context, lbMode, false);
            await presence.UserStats();

            var (status, beatmap) = await BeatmapManager.Get(beatmapChecksum, setId: setId);

            switch (status)
            {
                case RankedStatus.NotSubmitted:
                    return Ok("-1|false");
                case RankedStatus.Approved:
                    var isLeaderboardModeCached = beatmap.LeaderboardCache.TryGetValue(lbMode, out var modeLeaderboard);

                    if (!isLeaderboardModeCached)
                        await beatmap.InitializeLeaderboard(mode);

                    var personalBest = modeLeaderboard[mode].TryGetValue(Base.UserCache[username].Id, out var score);
                    var personalBestString = string.Empty;

                    if (personalBest)
                        personalBestString = score.ToString(beatmap.Status);

                    return Ok($"{beatmap.ToString(mode, lbMode)}\n"
                              + $"{(beatmap.Status == RankedStatus.LatestPending ? string.Empty : personalBestString)}\n"
                              + $"{(beatmap.Status == RankedStatus.LatestPending ? string.Empty : string.Join("\n", beatmap.LeaderboardFormatted[lbMode][mode]))}");
                case RankedStatus.NeedUpdate:
                    return Ok("1|false");
                default:
                    return Ok("-1|false");
            }
        }

        [HttpGet("maps/{fileName}")]
        public async Task<IActionResult> DownloadOsu(string fileName)
        {
            var beatmap = (await BeatmapManager.Get("no", fileName)).Item2;
            if (beatmap is not null)
            {
                var file = System.IO.File.OpenRead(
                    $"./data/beatmaps/{await Calculator.GetBeatmap(beatmap.FileChecksum)}.osu");
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