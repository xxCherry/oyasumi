#define MERGE_BEATMAPS
#define MERGE_SCORES
//#define FIX_REPLAYS

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using RippleDatabaseMerger.Database;
using RippleDatabaseMerger.Enums;

namespace RippleDatabaseMerger
{
    internal class Base
    {
        private static Dictionary<int, int> _rippleIdToOyasumi = new();
        private static ConcurrentDictionary<string, string> _replayHashByScoreHash = new();

        private static Privileges Convert(RipplePrivileges p)
        {
            var converted = (Privileges)0;
            if ((p & RipplePrivileges.UserNormal) != 0)
                converted |= Privileges.Normal;
            if ((p & RipplePrivileges.AdminManageUsers) != 0)
                converted |= Privileges.ManageUsers;
            if ((p & RipplePrivileges.AdminManageBeatmaps) != 0)
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

            var connectionString = args[0];
            var rippleReplayPath = args[1];
            var oyasumiReplayPath = args[2];
            var rippleRelaxReplayPath = string.Empty;

            if (args.Length == 4)
                rippleRelaxReplayPath = args[3];

            var builder = new DbContextOptionsBuilder<OyasumiDbContext>().UseMySql(
                $"server=localhost;database={Config.Properties.Database};" +
                $"user={Config.Properties.Username};password={Config.Properties.Password};");

            var oContext = new OyasumiDbContext(builder.Options);
            var rContext =
                new RippleDbContext(new DbContextOptionsBuilder<RippleDbContext>().UseMySql(connectionString).Options);
#if !FIX_REPLAYS
            Console.WriteLine("Users merging...");
            var rUsers = await rContext.Users.AsNoTracking().ToListAsync();
            var stats = await rContext.Stats.AsNoTracking().ToListAsync();
            var relaxStats = await rContext.RelaxStats.AsNoTracking().ToListAsync();

            foreach (var user in rUsers.Where(x => x.Id != 999))
            {
                var oUser = new User
                {
                    Username = user.Name,
                    Password = string.Empty,
                    Country = "XX",
                    Privileges = Convert(user.Privileges),
                    JoinDate = Time.UnixTimestampFromDateTime(user.JoinTimestamp),
                    Email = user.Email,
                };

                var vanillaStats = stats.FirstOrDefault(x => x.id == user.Id);
                var sRelaxStats = rContext.RelaxStats.AsNoTracking().FirstOrDefault(x => x.id == user.Id);
                
                await oContext.Users.AddAsync(oUser);
                await oContext.SaveChangesAsync();
                
                await oContext.VanillaStats.AddAsync(new() 
                {
                    TotalScoreOsu = vanillaStats.total_score_std,
                    TotalScoreTaiko = vanillaStats.total_score_taiko,
                    TotalScoreCtb = vanillaStats.total_score_ctb,
                    TotalScoreMania = vanillaStats.total_score_mania,
                    
                    RankedScoreOsu = vanillaStats.ranked_score_std,
                    RankedScoreTaiko = vanillaStats.ranked_score_taiko,
                    RankedScoreCtb = vanillaStats.ranked_score_ctb,
                    RankedScoreMania = vanillaStats.ranked_score_mania,

                    AccuracyOsu = vanillaStats.avg_accuracy_std / 100,
                    AccuracyTaiko = vanillaStats.avg_accuracy_taiko / 100,
                    AccuracyCtb = vanillaStats.avg_accuracy_ctb / 100,
                    AccuracyMania = vanillaStats.avg_accuracy_mania / 100,
                    
                    PlaycountOsu = vanillaStats.playcount_std,
                    PlaycountTaiko = vanillaStats.playcount_taiko,
                    PlaycountCtb = vanillaStats.playcount_ctb,
                    PlaycountMania = vanillaStats.playcount_mania
                });
                
                await oContext.RelaxStats.AddAsync(new () 
                {
                    TotalScoreOsu = sRelaxStats.total_score_std,
                    TotalScoreTaiko = sRelaxStats.total_score_taiko,
                    TotalScoreCtb = sRelaxStats.total_score_ctb,
                    TotalScoreMania = sRelaxStats.total_score_mania,
                    
                    RankedScoreOsu = sRelaxStats.ranked_score_std,
                    RankedScoreTaiko = sRelaxStats.ranked_score_taiko,
                    RankedScoreCtb = sRelaxStats.ranked_score_ctb,
                    RankedScoreMania = sRelaxStats.ranked_score_mania,

                    AccuracyOsu = sRelaxStats.avg_accuracy_std / 100,
                    AccuracyTaiko = sRelaxStats.avg_accuracy_taiko / 100,
                    AccuracyCtb = sRelaxStats.avg_accuracy_ctb / 100,
                    AccuracyMania = sRelaxStats.avg_accuracy_mania / 100,
                    
                    PlaycountOsu = sRelaxStats.playcount_std,
                    PlaycountTaiko = sRelaxStats.playcount_taiko,
                    PlaycountCtb = sRelaxStats.playcount_ctb,
                    PlaycountMania = sRelaxStats.playcount_mania
                }); 
                
                await oContext.RipplePasswords.AddAsync(new()
                {
                    UserId = oUser.Id,
                    Password = user.Password,
                    Salt = user.Salt
                });
                await oContext.SaveChangesAsync();
                
               _rippleIdToOyasumi.Add(user.Id, oUser.Id);
            }
            Console.WriteLine("Users merged...");
#if MERGE_BEATMAPS
            Console.WriteLine("Time for beatmaps...");
            var rBeatmaps = rContext.Beatmaps.AsNoTracking();
            
            foreach (var beatmap in rBeatmaps)
            {
                if (oContext.Beatmaps.Any(x => x.BeatmapMd5 == beatmap.Checksum))
                    continue;
                
                var oBeatmap = await BeatmapManager.Get(beatmap.Checksum,"", 0);
                if (oBeatmap.Item1 == RankedStatus.NotSubmitted || oBeatmap.Item1 == RankedStatus.NeedUpdate)
                    continue;

                oBeatmap.Item2.Status = beatmap.Status;
                var dbMap = oBeatmap.Item2.ToDb();

                await oContext.Beatmaps.AddAsync(dbMap);
                await oContext.SaveChangesAsync();
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
                    Console.WriteLine("User not found, skipping...");
                    continue;
                }

                var completed = Convert(score.Completed);

                if (completed == CompletedStatus.Failed)
                    continue;
                var timeParsed = int.TryParse(score.Time, out var time);
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
                    Accuracy = score.Accuracy,
                    TotalScore = score.Score,
                    MaxCombo = score.MaxCombo,
                    Date = timeParsed ? Time.UnixTimestampFromDateTime(time) : DateTime.Now.AddDays(-10),
                    Mods = (Mods) score.Mods,
                    PlayMode = (PlayMode) score.PlayMode,
                    Completed = completed
                };
                convertedScore.Relaxing = (convertedScore.Mods & Mods.Relax) != 0;
                convertedScore.PerformancePoints = await Calculator.CalculatePerformancePoints(convertedScore);

                var scoreReplayPath = Path.Combine(rippleReplayPath, $"replay_{score.Id}.osr");

                if (File.Exists(scoreReplayPath))
                {
                    await using var osr = File.OpenRead(scoreReplayPath);
                    await using var ms = new MemoryStream();
                    await osr.CopyToAsync(ms);
                    
                    convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                    var oyasumiScoreReplayPath = Path.Combine(oyasumiReplayPath, $"{convertedScore.ReplayChecksum}.osr");
                    
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
                        Console.WriteLine("User not found, skipping...");
                        continue;
                    }

                    var completed = Convert(score.Completed);
                    if (completed == CompletedStatus.Failed)
                        continue;

                    var timeParsed = int.TryParse(score.Time, out var time);
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
                        Accuracy = score.Accuracy,
                        TotalScore = score.Score,
                        MaxCombo = score.MaxCombo,
                        Date = timeParsed ? Time.UnixTimestampFromDateTime(time) : DateTime.Now.AddDays(-10),
                        Mods = (Mods) score.Mods,
                        PlayMode = (PlayMode) score.PlayMode,
                        Completed = Convert(score.Completed)
                    };

                    convertedScore.Relaxing = (convertedScore.Mods & Mods.Relax) != 0;
                    convertedScore.PerformancePoints = await Calculator.CalculatePerformancePoints(convertedScore);

                    var scoreReplayPath = Path.Combine(rippleRelaxReplayPath, $"replay_{score.Id}.osr");

                    if (File.Exists(scoreReplayPath))
                    {
                        await using var osr = File.OpenRead(scoreReplayPath);
                        await using var ms = new MemoryStream();
                        await osr.CopyToAsync(ms);

                        convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                        var oyasumiScoreReplayPath = Path.Combine(oyasumiReplayPath, $"{convertedScore.ReplayChecksum}.osr");

                        if (!File.Exists(oyasumiScoreReplayPath))
                            File.Copy(scoreReplayPath, oyasumiScoreReplayPath);
                    }

                    await oContext.Scores.AddAsync(convertedScore);
                }
            }

            await oContext.SaveChangesAsync(); 

            Console.WriteLine("Scores merged...");
#endif
#endif
#if FIX_REPLAYS
            
            foreach (var file in Directory.EnumerateFiles(oyasumiReplayPath))
            {
                await using (var stream = File.OpenRead(file))
                {
                    try
                    {
                        GenerateScoreHash(stream, Path.GetFileName(file.Replace(".osr", "")));
                    }
                    catch
                    {
                    }
                }
            }


            var dirInfo = new DirectoryInfo(oyasumiReplayPath);
            var files = dirInfo.GetFiles().OrderBy(f => f.LastWriteTime).ToList();

            
            var id = 0;

            bool validate(DateTime t1, DateTime t2)
            {
                return t1.Year == t2.Year && t1.Month == t2.Month && t1.Day == t2.Day && t1.Hour == t2.Hour &&
                       t1.Minute == t2.Minute && t1.Second >= t2.Second && t1.Second <= 60;
            }

            await using (var db = MySqlProvider.GetDbConnection())
            {
                var scores = await db.QueryAsync("SELECT * FROM Scores");

                foreach (var score in scores)
                {
                    await db.ExecuteAsync($"UPDATE Scores SET Id = {++id} WHERE Id = {score.Id}");
                }
            }

            var scores2 = oContext.Scores.AsNoTracking();
            await using (var db = MySqlProvider.GetDbConnection())
            {
                foreach (var score in scores2)
                {
                    try
                    {
                        await db.ExecuteAsync(
                            $"UPDATE Scores SET ReplayChecksum = NULL WHERE Id = {score.Id}");
                        if (score.ReplayChecksum is null)
                        {
                            var file = files.FirstOrDefault(x => validate(x.LastWriteTime, score.Date));
                            if (file is not null)
                                await db.ExecuteAsync(
                                    $"UPDATE Scores SET ReplayChecksum = '{file.Name.Split('.')[0]}' WHERE Id = {score.Id}");
                        }
                    }
                    catch
                    {

                    }
                }
            }


            await oContext.SaveChangesAsync();
#endif
            Thread.Sleep(3000);
        }
        
        public static void GenerateScoreHash(Stream replayFile, string replayMd5)
        {

            /*var str = $"{replay.Count300 + replay.Count100}{replay.BeatmapMD5Hash}{replay.CountMiss}{replay.CountGeki}{replay.CountKatu}{replay.ReplayTimestamp}{replay.Mods}";
            var scoreChecksum = Crypto.ComputeHash(str);
            
            _replayHashByScoreHash.TryAdd(scoreChecksum, replayMd5); */
        }
        
        public static IEnumerable<string> EnumerateFilesParallel(string path)
        {
            return Directory.EnumerateFiles(path).AsParallel();
        }
    }
}