﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using oyasumi.Attributes;
using oyasumi.Chat.Objects;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Chat
{
    public static class Commands
    {
        [Command("last", "Returns information about your last score", true, Privileges.Normal, 0)]
        public static async Task Last(Presence pr, string channel, string message, string[] args)
        {
            if (pr.LastScore is null)
            {
                await ChannelManager.BotMessage(pr, channel, $"Can't retrieve last score.");
                return;
            }

            await ChannelManager.BotMessage(pr, channel,
                $"[https://osu.ppy.sh/b/{pr.LastScore.Beatmap.Id} {pr.LastScore.Beatmap.Name}] " +
                $"{Math.Round(pr.LastScore.Beatmap.Metadata.Stars, 2)}★, " +
                $"{Math.Round(pr.LastScore.Accuracy * 100, 2)}%, " +
                $"{Math.Round(pr.LastScore.PerformancePoints, 2)}pp");
        }
        
        [Command("map", "Set the specified status to last /np'ed map", true, Privileges.ManageBeatmaps, 2)]
        public static async Task MapRanking(Presence pr, string channel, string message, string[] args)
        {
            if (!new[] {"rank", "love", "unrank"}.Any(args[0].Equals) || !new[] {"set", "map"}.Any(args[1].Equals))
            {
                await ChannelManager.BotMessage(pr, channel, $"Invalid syntax: !map <rank/unrank/love> <map/set>");
                return;
            }

            if (pr.LastNp is null)
            {
                await ChannelManager.BotMessage(pr, channel, $"Please /np first.");
                return;
            }

            var beatmap = pr.LastNp;
            var isSet = args[1] == "set";
            
            var status = args[0] switch
            {
                "rank" => RankedStatus.Ranked,
                "love" => RankedStatus.Loved,
                "unrank" => RankedStatus.LatestPending
            };

            if (isSet)
            {
                var beatmaps = BeatmapManager.Beatmaps.Values.Where(x => x.SetId == beatmap.SetId);
                foreach (var b in beatmaps)
                    b.Status = status;
            }
            else
            {
                beatmap.Status = status;
            }

            if (beatmap.Status == RankedStatus.LatestPending)
                beatmap.ClearLeaderboard();

            Base.BeatmapDbStatusUpdate.Enqueue(new()
            {
                Beatmap = beatmap,
                IsSet = isSet
            });

            await ChannelManager.BotMessage(pr, channel, $"{pr.Username} just {args[0]}ed the beatmap: " +
                                                         $"[https://osu.ppy.sh/b/{beatmap.Id} {beatmap.Name}]");
        }

        [Command("fds", "Removes duplicated scores", true, Privileges.ManageUsers, 0)]
        public static async Task FixDuplicateScores(Presence pr, string channel, string message, string[] args)
        {
            await ChannelManager.BotMessage(pr, channel, "Start cleaning duplicate scores...");
            await ChannelManager.BotMessage(pr, channel, "Done!");
        }

        [Command("repp", "Recalculate performance points for scores", true, Privileges.ManageUsers, 0)]
        public static async Task RecalculatePerformance(Presence pr, string channel, string message, string[] args)
        {
            await ChannelManager.BotMessage(pr, channel, "Done!");
        }

        [Command("uban", "Un/ban specified player", true, Privileges.ManageUsers, 1, true, onArgsPushed: "UBan_OnArgsPushed")]
        public static async Task UBan(Presence pr, string channel, string message, string[] args)
        {
            var user = DbContext.Users[args[0]];

            if (!user.Banned())
            {
                await ChannelManager.BotMessage(pr, channel, $"Bye, bye, {user.Username}");
                user.Privileges &= ~Privileges.Normal;

                DbContext.Users[user.Id].Privileges &= ~Privileges.Normal;

                var target = PresenceManager.GetPresenceById(user.Id);

                if (target is not null)
                {
                    await target.LoginReply(LoginReplies.WrongCredentials);
                    await target.Notification("Your account is banned.");
                }
                
                // TODO: Replace with SmartThreadPool
                new Thread(async () =>
                {
                    IEnumerable<DbScore> scores = null;
                    /*await using (var db = MySqlProvider.GetDbConnection())
                    {
                        scores = await db.QueryAsync<DbScore>($"SELECT * FROM Scores " + 
                                                              $"WHERE UserId = {user.Id} " +
                                                              $"AND Completed = {(int)CompletedStatus.Best}");
                    }*/
                    
                    foreach (var score in scores)
                    {
                        var lbMode = score.Relaxing ? LeaderboardMode.Relax : LeaderboardMode.Vanilla;
                        var beatmap = await BeatmapManager.Get(score.FileChecksum, "", 0, true, score.PlayMode);

                        if (beatmap is not null)
                            beatmap.UpdateLeaderboard(lbMode, score.PlayMode);
                    }
                    
                }).Start();
                
            }
            else
            {
                await ChannelManager.BotMessage(pr, channel, $"Welcome back, {user.Username}");
                user.Privileges |= Privileges.Normal;

                new Thread(async () =>
                {
                    IEnumerable<DbScore> scores = null;
                    /*await using (var db = MySqlProvider.GetDbConnection())
                    {
                        scores = await db.QueryAsync<DbScore>($"SELECT * FROM Scores " + 
                                                              $"WHERE UserId = {user.Id} " +
                                                              $"AND Completed = {(int)CompletedStatus.Best}");
                    }
                    */
                    foreach (var score in scores)
                    {
                        var lbMode = score.Relaxing ? LeaderboardMode.Relax : LeaderboardMode.Vanilla;
                        var beatmap = await BeatmapManager.Get(score.FileChecksum, "", 0, true, score.PlayMode);
                        if (beatmap is not null)
                            beatmap.UpdateLeaderboard(lbMode, score.PlayMode);
                    }

                }).Start();
            }
        }

        // Experimental command (Unused)
        [Command("mdel", "Deletes your last message", true, Privileges.ManageUsers)]
        public static async Task DeleteMessage(Presence pr, string channel, string message, string[] args)
        {
            var chan = ChannelManager.Channels[channel];

            pr.SubPresences.TryPop(out var lastMessage);

            foreach (var presence in chan.Presences.Values)
                await presence.UserSilence(lastMessage);
        }
    }
}