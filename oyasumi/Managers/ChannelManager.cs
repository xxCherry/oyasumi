using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using oyasumi.Layouts;

namespace oyasumi.Managers
{
    public static class ChannelManager
    {
        public static Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();

        public static void JoinChannel(this Presence pr, string channelName)
        {
            if (!Channels.TryGetValue(channelName, out var channel)) // Presence tried to join non-existent channel
                return;

            if (pr is null) // Presence is not online
                return;

            pr.ChatChannelJoinSuccess(channel.Name);

            channel.UserCount++;
            pr.Channels.Add(channel);
            channel.Presences.Add(pr);
        }

        public static void LeaveChannel(this Presence pr, string channelName)
        {
            if (!Channels.TryGetValue(channelName, out var channel)) // Presence tried to join non-existent channel
                return;

            if (pr is null) // Presence is not online
                return;

            channel.UserCount--;
            pr.Channels.Remove(channel);
            channel.Presences.Remove(pr);
        }

        public static void SendMessage(string sender, string message, string rawTarget, int id, bool isPublic) // Mostly used for dummy presences
        {
            if (!isPublic)
            {
                var target = PresenceManager.GetPresenceByName(rawTarget);
                
                if (target is not null)
                    target.ChatMessage(sender, message, rawTarget, id);
            }
            else
            {
                if (!Channels.TryGetValue(rawTarget, out var channel)) // Presence tried to send message to non-existent channel
                    return;

                foreach (var prRaw in channel.Presences)
                {
                    var pr = (Presence)prRaw;
                    if (pr.Id != id)
                        pr.ChatMessage(sender, message, rawTarget, id);
                }
            }
        }

        public static void SendMessage(Presence sender, string message, string rawTarget, bool isPublic)
        {
            if (!isPublic)
            {
                var target = PresenceManager.GetPresenceByName(rawTarget);

                if (target is not null)
                    target.ChatMessage(sender, message, rawTarget);
            }
            else
            {
                if (!Channels.TryGetValue(rawTarget, out var channel)) // Presence tried to send message to non-existent channel
                    return;

                if (!channel.Presences.Contains(sender)) // Presence tried to send message but they aren't in that channel
                    return;

                foreach (var prRaw in channel.Presences)
                {
                    var pr = (Presence)prRaw;
                    if (pr != sender)
                        pr.ChatMessage(sender, message, rawTarget);
                }
            }
        }
    }
}
