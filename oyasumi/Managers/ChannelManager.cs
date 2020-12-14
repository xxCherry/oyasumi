using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using oyasumi.Objects;
using oyasumi.Layouts;
using System.Threading.Tasks;
using oyasumi.Attributes;
using oyasumi.Enums;
using oyasumi.Utilities;

namespace oyasumi.Managers
{
    public static class ChannelManager
    {
        public static ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();

        public static async Task JoinChannel(this Presence pr, string channelName)
        {
            if (!Channels.TryGetValue(channelName, out var channel)) // Presence tried to join non-existent channel
                return;

            if (pr is null) // Presence is not online
                return;

            await pr.ChatChannelJoinSuccess(channel.Name);

            channel.UserCount++;
            pr.Channels.TryAdd(channel.RawName, channel);
            channel.Presences.TryAdd(pr.Id, pr);
        }

        public static async Task LeaveChannel(this Presence pr, string channelName, bool force = false)
        {
            if (!Channels.TryGetValue(channelName, out var channel)) // Presence tried to join non-existent channel
                return;

            if (pr is null) // Presence is not online
                return;

            channel.UserCount--;
            pr.Channels.TryRemove(channel.RawName, out _);
            channel.Presences.TryRemove(pr.Id, out _);

            if (force)
                await pr.RevokeChannel(channel.Name);
        }

        public static async Task BotMessage(Presence reciever, string channel, string message)
        {
            var isPublic = channel.StartsWith("#");
            await SendMessage("oyasumi", message, isPublic ? channel : reciever.Username, int.MaxValue, isPublic);
        }
        
        public static async Task SendMessage(string sender, string message, string rawTarget, int id, bool isPublic) // Mostly used for dummy presences
        {
            if (!isPublic)
            {
                var target = PresenceManager.GetPresenceByName(rawTarget);
                
                if (target is not null)
                    await target.ChatMessage(sender, message, rawTarget, id);
            }
            else
            {
                if (!Channels.TryGetValue(rawTarget, out var channel)) // Presence tried to send message to non-existent channel
                    return;

                foreach (var pr in channel.Presences.Values)
                {
                    if (pr.Id != id)
                        await pr.ChatMessage(sender, message, rawTarget, id);
                }
            }
        }

        public static async Task SendMessage(Presence sender, string message, string rawTarget, bool isPublic)
        {
            if (!isPublic)
            {
                var target = PresenceManager.GetPresenceByName(rawTarget);

                if (message.StartsWith("!"))
                {
                    var spaceIndex = message.IndexOf(' ');
                    var cmdString = message[spaceIndex == -1 ? 1.. : 1..spaceIndex]; // from start (without prefix) to first space
                    if (!Base.CommandCache.TryGetValue(cmdString, out var command))
                    {
                        var meth = Base.Types
                            .SelectMany(type => type.GetMethods())
                            .FirstOrDefault(m => m.GetCustomAttribute<CommandAttribute>()?.Command == cmdString);
                        var attr = meth.GetCustomAttributes<CommandAttribute>().ElementAt(0);

                        command = new()
                        {
                            Executor = ReflectionUtils.GetExecutor(meth), 
                            IsPublic = attr.IsPublic,
                            RequiredArgs = attr.RequiredArgs,
                            Privileges = attr.PrivilegesRequired
                        };

                        Base.CommandCache.TryAdd(cmdString, command);
                    }

                    if ((sender.Privileges & command.Privileges) > 0)
                    {
                        var cmdArgs = spaceIndex == -1 
                            ? Array.Empty<string>() 
                            : message[(spaceIndex + 1)..]
                                .Split(' ');
                        
                        if (command.RequiredArgs > -1 && cmdArgs.Length == command.RequiredArgs || command.RequiredArgs == -1)
                            command.Executor.Execute(null, new object[] {sender, rawTarget, message, cmdArgs});
                        else if (cmdArgs.Length > command.RequiredArgs)
                            await BotMessage(sender, target.Username, $"Too many arguments for !{cmdString}");
                        else if (cmdArgs.Length < command.RequiredArgs)
                            await BotMessage(sender, target.Username, $"Too few arguments for !{cmdString}");
                    }
                }

                if (target is not null)
                    await target.ChatMessage(sender, message, rawTarget);
            }
            else
            {
                if (!Channels.TryGetValue(rawTarget, out var channel)) // Presence tried to send message to non-existent channel
                    return;

                if (!channel.Presences.Values.Contains(sender)) // Presence tried to send message but they aren't in that channel
                    return;

                if (!channel.PublicWrite)
                    return;
                
                if (message.StartsWith("!"))
                {
                    var spaceIndex = message.IndexOf(' ');
                    var cmdString = message[spaceIndex == -1 ? 1.. : 1..spaceIndex]; // from start (without prefix) to first space
                    
                    if (!Base.CommandCache.TryGetValue(cmdString, out var command))
                    {
                        var meth = Base.Types
                            .SelectMany(type => type.GetMethods())
                            .FirstOrDefault(m => m.GetCustomAttribute<CommandAttribute>()?.Command == cmdString);
                        var attr = meth.GetCustomAttributes<CommandAttribute>().ElementAt(0);
                        
                        command = new()
                        {
                            Executor = ReflectionUtils.GetExecutor(meth), 
                            IsPublic = attr.IsPublic,
                            RequiredArgs = attr.RequiredArgs,
                            Privileges = attr.PrivilegesRequired
                        };

                        
                        Base.CommandCache.TryAdd(cmdString, command);
                    }

                    if ((sender.Privileges & command.Privileges) > 0)
                    {
                        if (command.IsPublic)
                        {
                            var cmdArgs = spaceIndex == -1
                                ? Array.Empty<string>()
                                : message[(spaceIndex + 1)..]
                                    .Split(' ');
                            
                            if (command.RequiredArgs > -1 && cmdArgs.Length == command.RequiredArgs || command.RequiredArgs == -1)
                                command.Executor.Execute(null, new object[] {sender, rawTarget, message, cmdArgs});
                            else if (cmdArgs.Length > command.RequiredArgs)
                                await BotMessage(sender, channel.Name, $"Too many arguments for !{cmdString}");
                            else if (cmdArgs.Length < command.RequiredArgs)
                                await BotMessage(sender, channel.Name, $"Too few arguments for !{cmdString}");
                        }
                    }
                }
                
                if (message.StartsWith('\x1' + @"ACTION is listening to"))
                {
                    var idPart = message.Split("/b/")[1];
                    sender.LastNp = BeatmapManager.Beatmaps[int.Parse(idPart[..idPart.IndexOf(' ')])];
                }
                
                foreach (var pr in channel.Presences.Values)
                {
                    if (pr != sender)
                        await pr.ChatMessage(sender, message, channel.Name);
                }
            }
        }
    }
}
