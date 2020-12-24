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
using System.Threading;
using oyasumi.Chat.Objects;
using oyasumi.Chat;
using static oyasumi.Chat.CommandEvents;

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
                    var command = GetOrAddCommand(message);

                    if ((sender.Privileges & command.Privileges) > 0)
                        await ExecuteCommand(sender, command, null, rawTarget, message);
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
                    var command = GetOrAddCommand(message);

                    if ((sender.Privileges & command.Privileges) > 0)
                    {
                        if (command.IsPublic)
                            await ExecuteCommand(sender, command, channel, rawTarget, message);
                    }
                }
                
                foreach (var pr in channel.Presences.Values)
                {
                    if (pr != sender)
                        await pr.ChatMessage(sender, message, channel.Name);
                }
            }

            if (!message.StartsWith("!"))
                await CheckScheduledCommands(sender, rawTarget, message);

            if (message.StartsWith('\x1' + @"ACTION is listening to"))
            {
                var idPart = message.Split("/b/")[1];
                sender.LastNp = BeatmapManager.Beatmaps[int.Parse(idPart[..idPart.IndexOf(' ')])];
            }
        }

        private static async Task ExecuteCommand(Presence sender, CommandItem command, Channel channel, string rawTarget, string message)
        {
            var spaceIndex = message.IndexOf(' ');
            var cmdString = message[spaceIndex == -1 ? 1.. : 1..spaceIndex]; // from start (without prefix) to first space
            var cmdArgs = spaceIndex == -1
                ? Array.Empty<string>()
                : message[(spaceIndex + 1)..]
                    .Split(' ');

            var isMatch = true;
            var isPublic = rawTarget.StartsWith("#");

            if (command.Filter is not null)
            {
                var filter = new Filter(command.Filter, sender);
                isMatch = filter.IsMatch();
            }

            // Special case for scheduled commands
            if (command.Scheduled)
            {
                // Run scheduled commands in separate thread
                // Because if we don't, the current thread will be
                // frozen for a while
                new Thread(() =>
                {
                    var action = typeof(CommandEvents).GetMethod(command.OnArgsPushed).CreateDelegate(typeof(OnArgsPushed));
                    sender.CommandQueue.Enqueue(new ScheduledCommand
                    {
                        Name = command.Name,
                        ArgsRequired = command.RequiredArgs,
                        Args = new string[command.RequiredArgs],
                        OnArgsPushed = (OnArgsPushed)action
                    });

                    // Wait until user messages
                    while (sender.WaitForCommandArguments(command.Name, out cmdArgs))
                        Thread.Sleep(100);

                    // Break command execution if something went wrong
                    // while checking arguments
                    if (cmdArgs == null)
                        return;

                    command.Executor.Execute(null, new object[] { sender, rawTarget, message, cmdArgs });
                }).Start();
            }
            else
            {
                if (command.RequiredArgs > -1 && isMatch && cmdArgs.Length == command.RequiredArgs || command.RequiredArgs == -1)
                    command.Executor.Execute(null, new object[] { sender, rawTarget, message, cmdArgs });
                else if (cmdArgs.Length > command.RequiredArgs)
                    await BotMessage(sender, isPublic ? channel.Name : rawTarget, $"Too many arguments for !{cmdString}");
                else if (cmdArgs.Length < command.RequiredArgs)
                    await BotMessage(sender, isPublic ? channel.Name : rawTarget, $"Too few arguments for !{cmdString}");
            }
        }

        private static async Task CheckScheduledCommands(Presence sender, string rawTarget, string message)
        {
            if (!sender.CommandQueue.IsEmpty)
            {
                if (sender.CommandQueue.TryPeek(out var command))
                {
                    if (command.ArgsRequired > 0)
                    {
                        var currentIndex = command.Args.Length - command.ArgsRequired;

                        command.Args[currentIndex] = message;
                        command.NoErrors = await command.OnArgsPushed(sender, rawTarget, currentIndex, message);

                        // If OnArgsPushed event returned false that means something went wrong, force break execution..
                        if (!command.NoErrors)
                        {
                            if (sender.CommandQueue.TryDequeue(out command))
                                sender.ProcessedCommands.TryAdd(command.Name, command);

                            return;
                        }
                        command.ArgsRequired--;
                    }


                    if (command.ArgsRequired == 0)
                    {
                        if (sender.CommandQueue.TryDequeue(out command))
                            sender.ProcessedCommands.TryAdd(command.Name, command);
                    }
                }
            }
        }

        private static CommandItem GetOrAddCommand(string message)
        {
            var spaceIndex = message.IndexOf(' ');
            var cmdString = message[spaceIndex == -1 ? 1.. : 1..spaceIndex]; // from start (without prefix) to first space

            if (!Base.CommandCache.TryGetValue(cmdString, out var command))
            {
                var meth = Base.Types
                    .SelectMany(type => type.GetMethods())
                    .FirstOrDefault(m => m.GetCustomAttribute<CommandAttribute>()?.Command == cmdString);
                var attr = meth.GetCustomAttribute<CommandAttribute>();

                command = new()
                {
                    Name = cmdString,
                    Executor = ReflectionUtils.GetExecutor(meth),
                    IsPublic = attr.IsPublic,
                    RequiredArgs = attr.RequiredArgs,
                    Privileges = attr.PrivilegesRequired,
                    Filter = attr.Filter,
                    Scheduled = attr.Scheduled,
                    OnArgsPushed = attr.OnArgsPushed
                };

                Base.CommandCache.TryAdd(cmdString, command);
            }
            return command;
        }
    }
}
