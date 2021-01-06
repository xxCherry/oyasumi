#define MERGE_BEATMAPS
#define MERGE_SCORES

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using gulagDatabaseMerger.Database;
using gulagDatabaseMerger.Enums;
using static System.Int32;
using System.Net;

namespace gulagDatabaseMerger
{
    internal static class Base
    {
        private static Dictionary<int, int> _rippleIdToOyasumi = new();

        private static Privileges Convert(GulagPrivileges p)
        {
            var converted = (Privileges)0;
            if ((p & GulagPrivileges.Normal) != 0)
                converted |= Privileges.Normal;
            if ((p & GulagPrivileges.Verified) != 0)
                converted |= Privileges.Verified;
            if ((p & GulagPrivileges.Admin) != 0)
                converted |= Privileges.ManageUsers;
            if ((p & GulagPrivileges.Mod) != 0)
                converted |= Privileges.ManageUsers;
            if ((p & GulagPrivileges.Nominator) != 0)
                converted |= Privileges.ManageBeatmaps;

            return converted;
        }

        private static CompletedStatus Convert(int completed) =>
            completed switch
            {
                2 => CompletedStatus.Submitted,
                3 => CompletedStatus.Best,
                _ => CompletedStatus.Failed
            };

        private static async Task Main(string[] args)
        {
            if (args.Length < 3)
                return;

            var rdbName = args[0];
            var dbuser = args[1];
            var pass = args[2];
            var gulagData = args[3];
            var oyasumiDir = args[4];
            var rippleRelaxReplayPath = string.Empty;

            Directory.SetCurrentDirectory(oyasumiDir);
            var oyasumiData = Path.Combine(oyasumiDir, "data");

            if (args.Length == 4)
                rippleRelaxReplayPath = args[5];

            var builder = new DbContextOptionsBuilder<OyasumiDbContext>().UseMySql(
                $"server=localhost;database={Config.Properties.Database};" +
                $"user={Config.Properties.Username};password={Config.Properties.Password};");
            var oContext = new OyasumiDbContext(builder.Options);
            var rContext = new GulagDbContext(new DbContextOptionsBuilder<GulagDbContext>().UseMySql(
                $"server=localhost;database={rdbName};" +
                $"user={dbuser};password={pass};").Options);
            Console.WriteLine("Users merging...");
            var rUsers = await rContext.Users.AsNoTracking().ToListAsync();
            var stats = await rContext.Stats.AsNoTracking().ToListAsync();

            foreach (var user in rUsers.Where(x => x.Id != 999))
            {
                try
                {
                    var oUser = new User
                    {
                        Username = user.Name,
                        UsernameSafe = user.SafeName,
                        Password = user.Password,
                        Country = user.Country,
                        Privileges = Convert(user.Privileges),
                        JoinDate = Time.UnixTimestampFromDateTime(user.JoinTimestamp),
                        Email = user.Email,
                    };

                    var vanillaStats = stats.FirstOrDefault(x => x.id == user.Id);

                    await oContext.Users.AddAsync(oUser);
                    await oContext.SaveChangesAsync();

                    await oContext.VanillaStats.AddAsync(new()
                    {
                        TotalScoreOsu = vanillaStats.tscore_vn_std,
                        TotalScoreTaiko = vanillaStats.tscore_vn_taiko,
                        TotalScoreCtb = vanillaStats.tscore_vn_catch,
                        TotalScoreMania = vanillaStats.tscore_vn_mania,

                        RankedScoreOsu = vanillaStats.rscore_vn_std,
                        RankedScoreTaiko = vanillaStats.rscore_vn_taiko,
                        RankedScoreCtb = vanillaStats.rscore_vn_catch,
                        RankedScoreMania = vanillaStats.rscore_vn_mania,

                        AccuracyOsu = vanillaStats.acc_vn_std / 100,
                        AccuracyTaiko = vanillaStats.acc_vn_taiko / 100,
                        AccuracyCtb = vanillaStats.acc_vn_catch / 100,
                        AccuracyMania = vanillaStats.acc_vn_mania / 100,

                        PlaycountOsu = vanillaStats.plays_vn_std,
                        PlaycountTaiko = vanillaStats.plays_vn_taiko,
                        PlaycountCtb = vanillaStats.plays_vn_catch,
                        PlaycountMania = vanillaStats.plays_vn_mania
                    });

                    await oContext.RelaxStats.AddAsync(new()
                    {
                        TotalScoreOsu = vanillaStats.tscore_rx_std,
                        TotalScoreTaiko = vanillaStats.tscore_rx_taiko,
                        TotalScoreCtb = vanillaStats.tscore_rx_catch,
                        TotalScoreMania = vanillaStats.tscore_vn_mania,

                        RankedScoreOsu = vanillaStats.rscore_rx_std,
                        RankedScoreTaiko = vanillaStats.rscore_rx_taiko,
                        RankedScoreCtb = vanillaStats.rscore_rx_catch,
                        RankedScoreMania = vanillaStats.rscore_vn_mania,

                        AccuracyOsu = vanillaStats.acc_rx_std / 100,
                        AccuracyTaiko = vanillaStats.acc_rx_taiko / 100,
                        AccuracyCtb = vanillaStats.acc_rx_catch / 100,
                        AccuracyMania = vanillaStats.acc_vn_mania / 100,

                        PlaycountOsu = vanillaStats.plays_rx_std,
                        PlaycountTaiko = vanillaStats.plays_rx_taiko,
                        PlaycountCtb = vanillaStats.plays_rx_catch,
                        PlaycountMania = vanillaStats.plays_vn_mania
                    });

                    await oContext.SaveChangesAsync();

                    _rippleIdToOyasumi.Add(user.Id, oUser.Id);
                }
                catch (NullReferenceException) { } 
            }
            Console.WriteLine("Users merged...");
#if MERGE_BEATMAPS
            Console.WriteLine("Time for beatmaps...");
            var bmPathGulag = Path.Combine(gulagData, "osu");
            var bmPathOyasumi = Path.Combine(oyasumiData, "beatmaps");
            var wc = new WebClient();
            var rBeatmaps = rContext.Beatmaps.AsNoTracking();
            
            foreach (var beatmap in rBeatmaps)
            {
                try
                {
                    if (oContext.Beatmaps.Any(x => x.BeatmapMd5 == beatmap.Checksum))
                        continue;

                    var dbMap = new DbBeatmap
                    {
                        BeatmapMd5 = beatmap.Checksum,
                        BeatmapId = beatmap.Id,
                        BeatmapSetId = beatmap.SetId,
                        Artist = beatmap.Artist,
                        Title = beatmap.Title,
                        Creator = beatmap.Creator,
                        DifficultyName = beatmap.Version,
                        PlayCount = beatmap.PlayCount,
                        PassCount = beatmap.PassCount,
                        Frozen = beatmap.Frozen,
                        BPM = beatmap.BPM,
                        ApproachRate = beatmap.AR,
                        CircleSize = beatmap.CS,
                        HPDrainRate = beatmap.HP,
                        OverallDifficulty = beatmap.OD,
                        Stars = beatmap.SR,
                        Status = beatmap.Status
                    };

                    await oContext.Beatmaps.AddAsync(dbMap);
                    await oContext.SaveChangesAsync();

                    var bpg = Path.Combine(bmPathGulag, $"{beatmap.Id}.osu");
                    var bpo = Path.Combine(bmPathOyasumi, $"{beatmap.Checksum}.osu");

                    if (!File.Exists(bpo))
                    {
                        if (File.Exists(bpg))
                            File.Copy(bpg, bpo);
                        else
                            wc.DownloadFile($"https://osu.ppy.sh/osu/{beatmap.Id}", bpo);
                    }
                }
                catch { }
            } 
            Console.WriteLine("Beatmaps merged!");
#endif
#if MERGE_SCORES
            Console.WriteLine("Merging scores...");
            var scores = (await rContext.Scores.AsNoTracking().ToListAsync()).Where(x => x.PlayMode == 0);
            foreach (var score in scores)
            {
                if (!_rippleIdToOyasumi.TryGetValue(score.UserId, out var newUserId))
                {
                    Console.WriteLine($"User {score.UserId} not found, skipping...");
                    continue;
                }

                var completed = Convert(score.Completed);

                if (completed == CompletedStatus.Failed)
                    continue;

                var convertedScore = new DbScore
                {
                    FileChecksum = score.BeatmapChecksum,
                    UserId = newUserId,
                    Count300 = score.Count300,
                    Count100 = score.Count100,
                    Count50 = score.Count50,
                    CountGeki = score.CountGeki,
                    CountKatu = score.CountKatu,
                    CountMiss = score.CountMiss,
                    Accuracy = score.Accuracy / 100,
                    TotalScore = score.Score,
                    MaxCombo = score.MaxCombo,
                    Date = score.Time,
                    Mods = (Mods)score.Mods,
                    PlayMode = (PlayMode)score.PlayMode,
                    Completed = completed
                };
                convertedScore.Relaxing = (convertedScore.Mods & Mods.Relax) != 0;
                convertedScore.PerformancePoints = await Calculator.CalculatePerformancePoints(convertedScore);
                var scoreReplayPath = Path.Combine(gulagData, "osr", $"{score.Id}.osr");

                if (File.Exists(scoreReplayPath))
                {
                    await using var osr = File.OpenRead(scoreReplayPath);
                    await using var ms = new MemoryStream();
                    await osr.CopyToAsync(ms);

                    convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                    var oyasumiScoreReplayPath = Path.Combine(oyasumiData, "osr", $"{convertedScore.ReplayChecksum}.osr");

                    if (!File.Exists(oyasumiScoreReplayPath))
                        File.Copy(scoreReplayPath, oyasumiScoreReplayPath);
                }

                await oContext.Scores.AddAsync(convertedScore);
            }

            if (rippleRelaxReplayPath != string.Empty)
            {
                var scoresRelax =
                    (await rContext.RelaxScores.AsNoTracking().ToListAsync()).Where(x => x.PlayMode == 0);
                foreach (var score in scoresRelax)
                {
                    if (!_rippleIdToOyasumi.TryGetValue(score.UserId, out var newUserId))
                    {
                        Console.WriteLine($"User {score.UserId} not found, skipping...");
                        continue;
                    }

                    var completed = Convert(score.Completed);
                    if (completed == CompletedStatus.Failed)
                        continue;

                    var convertedScore = new DbScore
                    {
                        FileChecksum = score.BeatmapChecksum,
                        UserId = newUserId,
                        Count300 = score.Count300,
                        Count100 = score.Count100,
                        Count50 = score.Count50,
                        CountGeki = score.CountGeki,
                        CountKatu = score.CountKatu,
                        CountMiss = score.CountMiss,
                        Accuracy = score.Accuracy / 100,
                        TotalScore = score.Score,
                        MaxCombo = score.MaxCombo,
                        Date = score.Time,
                        Mods = (Mods)score.Mods,
                        PlayMode = (PlayMode)score.PlayMode,
                        Completed = Convert(score.Completed)
                    };

                    convertedScore.Relaxing = (convertedScore.Mods & Mods.Relax) != 0;
                    convertedScore.PerformancePoints = await Calculator.CalculatePerformancePoints(convertedScore);

                    var scoreReplayPath = Path.Combine(rippleRelaxReplayPath, $"{score.Id}.osr");

                    if (File.Exists(scoreReplayPath))
                    {
                        await using var osr = File.OpenRead(scoreReplayPath);
                        await using var ms = new MemoryStream();
                        await osr.CopyToAsync(ms);

                        convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                        var oyasumiScoreReplayPath = Path.Combine(oyasumiData, "osr", $"{convertedScore.ReplayChecksum}.osr");

                        if (!File.Exists(oyasumiScoreReplayPath))
                            File.Copy(scoreReplayPath, oyasumiScoreReplayPath);
                    }

                    await oContext.Scores.AddAsync(convertedScore);
                }
            }

            await oContext.SaveChangesAsync();

            Console.WriteLine("Scores merged...");
#endif

                    Thread.Sleep(3000);
        }
    }
}