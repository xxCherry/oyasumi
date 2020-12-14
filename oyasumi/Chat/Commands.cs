using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Attributes;
using oyasumi.Enums;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Chat
{
    public class Commands
    {
        [Command("last", "Returns information about your last score", true, Privileges.Normal, 0)]
        public static async Task Last(Presence pr, string channel, string message, string[] args)
        {
            if (pr.LastScore is null)
            {
                await ChannelManager.BotMessage(pr, channel, $"Can't retrieve last score.");
                return;
            }

            await ChannelManager.BotMessage(pr, channel, $"[https://osu.ppy.sh/b/{pr.LastScore.Beatmap.Id} {pr.LastScore.Beatmap.BeatmapName}] " +
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

            beatmap.Status = args[0] switch
            {
                "rank" => RankedStatus.Ranked,
                "love" => RankedStatus.Loved,
                "unrank" => RankedStatus.LatestPending
            };

            if (beatmap.Status == RankedStatus.LatestPending)
                beatmap.ClearLeaderboard(); 
            
            Base.BeatmapDbStatusUpdate.Enqueue(beatmap);
            
            await ChannelManager.BotMessage(pr, channel, $"{pr.Username} just {args[0]}ed the beatmap: " +
                                                         $"[https://osu.ppy.sh/b/{beatmap.Id} {beatmap.BeatmapName}]");
        }

        [Command("ban", "Bans specified player", true, Privileges.ManageUsers, 1)]
        public static async Task Ban(Presence pr, string channel, string message, string[] args)
        {
            var user = Base.UserCache[args[0]];
            
            if (user is null)
            {
                await ChannelManager.BotMessage(pr, channel, $"User not found.");
                return;
            }
            
            user.Privileges |= ~Privileges.Normal;
            Base.UserDbUpdate.Enqueue(user);

            var target = PresenceManager.GetPresenceById(user.Id);
            
            if (target is not null)
            {
                await target.LoginReply(LoginReplies.WrongCredentials);
                await target.Notification("Your account is banned.");
            }
            
            await ChannelManager.BotMessage(pr, channel, $"Bye, bye, {user.Username}");
        }
    }
}   