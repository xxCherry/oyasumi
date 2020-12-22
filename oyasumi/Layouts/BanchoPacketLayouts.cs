using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Layouts
{
    // TODO: probably i should rename this
    public static class BanchoPacketLayouts
    {
        public static async Task ProtocolVersion(this Presence p, int version)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerBanchoVersion
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(version);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task LoginReply(this Presence p, LoginReplies reply)
        {
            await p.LoginReply((int)reply);
        }
        
        public static async Task LoginReply(this Presence p, int reply)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerLoginReply
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write((int)reply);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task<byte[]> LoginReplyAsync(LoginReplies reply)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerLoginReply
            };

            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write((int)reply);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            var pWriter = new PacketWriter();
            await pWriter.Write(packet);

            return pWriter.ToBytes();
        }
        
        public static async Task<byte[]> NotificationAsync(string notification)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerNotification
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(notification);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            var pWriter = new PacketWriter();
            await pWriter.Write(packet);

            return pWriter.ToBytes();
        }

        public static async Task<byte[]> BannedError()
        {
            var notificationBytes = await NotificationAsync("You're banned from the server.");
            var wrongCredsBytes = await LoginReplyAsync(LoginReplies.WrongCredentials);
            
            var pWriter = new PacketWriter();

            await pWriter.Write(notificationBytes);
            await pWriter.Write(wrongCredsBytes);

            return pWriter.ToBytes();
        }
        
        public static async Task Notification(this Presence p, string notification)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerNotification
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(notification);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task UserPresence(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresence
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(p.Id);
            writer.Write(p.Username);
            writer.Write((byte)(p.Timezone + 24));
            writer.Write(p.CountryCode);
            writer.Write((int)p.BanchoPermissions);
            writer.Write(p.Longitude);
            writer.Write(p.Latitude);
            writer.Write(p.Rank);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task UserLogout(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserQuit
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task UserPresence(this Presence p, Presence other)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresence
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(other.Id);
            writer.Write(other.Username);
            writer.Write((byte)(other.Timezone + 24));
            writer.Write(other.CountryCode);
            writer.Write((int) BanchoPermissions.Peppy);
            writer.Write(other.Longitude);
            writer.Write(other.Latitude);
            writer.Write(other.Rank);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task<byte[]> BanchoRestart(int timeout)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerRestart
            };

            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(timeout);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            var pWriter = new PacketWriter();
            await pWriter.Write(packet);

            return pWriter.ToBytes();
        }
  
        public static byte[] PresenceStatus(this Presence p, SerializationWriter writer)
        {
            return ((MemoryStream) writer.BaseStream).ToArray();
        }
        public static async Task UserStats(this Presence p, Presence other)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserData
            };

            await using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(other.Id);
            writer.Write((byte)other.Status.Status);
            writer.Write(other.Status.StatusText);
            writer.Write(other.Status.BeatmapChecksum);
            writer.Write((uint)other.Status.CurrentMods);
            writer.Write((byte)other.Status.CurrentPlayMode);
            writer.Write(other.Status.BeatmapId);
            writer.Write(other.RankedScore);
            writer.Write((float)other.Accuracy);
            writer.Write(other.PlayCount);
            writer.Write(other.TotalScore);
            writer.Write(other.Rank);
            writer.Write(other.Performance);
            
            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        public static async Task UserStats(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserData
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(p.Id);
            writer.Write((byte)p.Status.Status);
            writer.Write(p.Status.StatusText);
            writer.Write(p.Status.BeatmapChecksum);
            writer.Write((uint)p.Status.CurrentMods);
            writer.Write((byte)p.Status.CurrentPlayMode);
            writer.Write(p.Status.BeatmapId);
            writer.Write(p.RankedScore);
            writer.Write((float)p.Accuracy);
            writer.Write(p.PlayCount);
            writer.Write(p.TotalScore);
            writer.Write(p.Rank);
            writer.Write(p.Performance);
            
            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task UserPermissions(this Presence p, BanchoPermissions perms)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPermissions
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write((int)perms);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task FriendList(this Presence p, int[] friendIds)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerFriendsList
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            friendIds ??= Array.Empty<int>();

            writer.Write((short)friendIds.Length);

            foreach (var t in friendIds)
                writer.Write(t);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            p.PacketEnqueue(packet);
        }

        public static async Task UserPresenceSingle(this Presence p, int userId)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresenceSingle
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(userId);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task ChatChannelListingComplete(this Presence p, int i)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelListingComplete
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(i);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task ChatChannelJoinSuccess(this Presence p, string channel)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelJoinSuccess
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(channel);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static async Task ChatChannelAvailable(this Presence p, string name, string topic, short userCount)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelAvailable
            };
            
            await using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(name);
            writer.Write(topic);
            writer.Write(userCount);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static async Task ChatMessage(this Presence p, string sender, string message, string target, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatMessage
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender);
            writer.Write(message);
            writer.Write(target);
            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task ChatMessage(this Presence p, Presence sender, string message, string target)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatMessage
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender.Username);
            writer.Write(message);
            writer.Write(target);
            writer.Write(sender.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task SpectatorJoined(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerSpectateSpectatorJoined
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task SpectatorLeft(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerSpectateSpectatorLeft
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task NewMatch(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchNew
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);
            
            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }


        public static void MatchJoinFail(this Presence p)
        {
            p.PacketEnqueue(new Packet
            {
                Type = PacketType.ServerMultiMatchJoinFail
            });
        }

        public static async Task MatchJoinSuccess(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchJoinSuccess
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task MatchUpdate(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchUpdate
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task MatchStart(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchStart
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task MatchPlayerFailed(this Presence p, int slotId)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiOtherFailed
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(slotId);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task MatchPlayerSkipped(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiSkipRequestOther
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(p.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task MatchTransferHost(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiHostTransfer
            };

            p.PacketEnqueue(packet);
        }

        public static async Task AllPlayersLoaded(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiAllPlayersLoaded
            };

            p.PacketEnqueue(packet);
        }

        public static async Task MatchInvite(this Presence p, Presence sender, string message)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiInvite
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender.Username);
            writer.Write(message);
            writer.Write(p.Username);
            writer.Write(sender.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static async Task RevokeChannel(this Presence p, string channel)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelRevoked
            };

            await using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(channel);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }
    }
}