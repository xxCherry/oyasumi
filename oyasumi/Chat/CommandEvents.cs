using oyasumi.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Objects;

namespace oyasumi.Chat
{
    public class CommandEvents
    {
        // If you want to break the execution of command
        // Just return false in the event method
        public delegate Task<bool> OnArgsPushed(Presence sender, string channel, int index, string arg);

        public static async Task<bool> UBan_OnArgsPushed(Presence sender, string channel, int index, string arg)
        {
            if (index == 0)
            {
                var user = Base.UserCache[arg];
                if (user is null)
                {
                    await ChannelManager.BotMessage(sender, channel, "User not found.");
                    return false;
                }
            }
            return true;
        }
    }
}
