using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using osu.Game.Screens.Play.HUD;
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
    internal static class Base
    {
        private static Dictionary<int, int> _rippleIdToOyasumi = new();
        
        private static Privileges Convert(RipplePrivileges p)
        {
            var converted = Privileges.Banned;
            if ((p & RipplePrivileges.UserNormal) != 0)
                converted |= Privileges.Normal;
            if ((p & RipplePrivileges.AdminManageUsers) != 0)
                converted |= Privileges.ManageUsers;
            if ((p & RipplePrivileges.AdminManageBeatmaps) != 0)
                converted |= Privileges.ManageBeatmaps;

            return converted;
        }
        
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
            var rContext = new RippleDbContext(new DbContextOptionsBuilder<RippleDbContext>().UseMySql(connectionString).Options);
   
            Console.WriteLine("Users merging...");
            var rUsers = rContext.Users.AsNoTracking();

            foreach (var user in rUsers.Where(x => x.Id != 999))
            {
                var oUser = new User
                {
                    Username = user.Name,
                    Privileges = Convert(user.Privileges),
                    Email = user.Email
                };
                
                await oContext.Users.AddAsync(oUser);
                await oContext.RipplePasswords.AddAsync(new()
                {
                    UserId = user.Id,
                    Password = user.Password,
                    Salt = user.Salt
                });
                
                await oContext.SaveChangesAsync();
                
                _rippleIdToOyasumi.Add(user.Id, oUser.Id);
            }
            Console.WriteLine("Users merged...");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Time for beatmaps...");
            var rBeatmaps = rContext.Beatmaps.AsNoTracking();

            foreach (var beatmap in rBeatmaps)
            {
                if (oContext.Beatmaps.Any(x => x.BeatmapMd5 == beatmap.Checksum))
                    continue;
                
                var oBeatmap = await BeatmapManager.Get(beatmap.Checksum,"", 0, oContext);
                if (oBeatmap.Item1 == RankedStatus.NotSubmitted || oBeatmap.Item1 == RankedStatus.NeedUpdate)
                    continue;

                oBeatmap.Item2.Status = beatmap.Status;
                var dbMap = oBeatmap.Item2.ToDb();

                await oContext.Beatmaps.AddAsync(dbMap);
                await oContext.SaveChangesAsync();
            }
            Console.WriteLine("Beatmaps merged!");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Merging scores...");
            foreach (var score in rContext.Scores)
            {
                var newUserId = _rippleIdToOyasumi[score.UserId];
                if (newUserId != 0)
                {
                    Console.WriteLine("User not found, skipping...");
                    continue;
                }

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
                    TotalScore = score.Score,
                    MaxCombo = score.MaxCombo,
                    Mods = (Mods)score.Mods,
                    PlayMode = (PlayMode)score.PlayMode,
                    Relaxing = false,
                    Flags = 0
                };

                var scoreReplayPath = Path.Combine(rippleReplayPath, $"replay_{score.Id}.osr");

                if (File.Exists(scoreReplayPath))
                {
                    await using var osr = File.OpenRead(scoreReplayPath);
                    await using var ms = new MemoryStream();
                    await osr.CopyToAsync(ms);
                    
                    convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                    var oyasumiScoreReplayPath = Path.Combine(oyasumiReplayPath, convertedScore.ReplayChecksum);
                    
                    if (!File.Exists(oyasumiScoreReplayPath)) 
                        File.Copy(scoreReplayPath, oyasumiScoreReplayPath);
                }

                await oContext.Scores.AddAsync(convertedScore);
            }

            if (rippleRelaxReplayPath != string.Empty)
            {
                foreach (var score in rContext.RelaxScores)
                {
                    var newUserId = _rippleIdToOyasumi[score.UserId];
                    if (newUserId != 0)
                    {
                        Console.WriteLine("User not found, skipping...");
                        continue;
                    }

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
                        TotalScore = score.Score,
                        MaxCombo = score.MaxCombo,
                        Mods = (Mods) score.Mods,
                        PlayMode = (PlayMode) score.PlayMode,
                        Relaxing = true,
                        Flags = 0
                    };

                    var scoreReplayPath = Path.Combine(rippleRelaxReplayPath, $"replay_{score.Id}.osr");

                    if (File.Exists(scoreReplayPath))
                    {
                        await using var osr = File.OpenRead(scoreReplayPath);
                        await using var ms = new MemoryStream();
                        await osr.CopyToAsync(ms);

                        convertedScore.ReplayChecksum = Crypto.ComputeHash(ms.ToArray());

                        var oyasumiScoreReplayPath = Path.Combine(oyasumiReplayPath, convertedScore.ReplayChecksum);

                        if (!File.Exists(oyasumiScoreReplayPath))
                            File.Copy(scoreReplayPath, oyasumiScoreReplayPath);
                    }

                    await oContext.Scores.AddAsync(convertedScore);
                }
            }

            Console.WriteLine("Scores merged...");

            Thread.Sleep(3000);
        }
    }
}