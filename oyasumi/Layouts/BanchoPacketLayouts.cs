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
        public static void ProtocolVersion(this Presence p, int version)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerBanchoVersion
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(version);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static void LoginReply(this Presence p, int reply)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerLoginReply
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(reply);

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

        public static void Notification(this Presence p, string notification)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerNotification
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(notification);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static void UserPresence(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresence
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(p.Id);
            writer.Write(p.Username);
            writer.Write((byte)(p.Timezone + 24));
            writer.Write(p.CountryCode);
            writer.Write((int) BanchoPermissions.Peppy);
            writer.Write(p.Longitude);
            writer.Write(p.Latitude);
            writer.Write(p.Rank);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static void UserLogout(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserQuit
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static void UserPresence(this Presence p, Presence other)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresence
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            
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
        public static void UserStats(this Presence p, Presence other)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserData
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            
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
        public static void UserStats(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserData
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            
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

        public static void UserPermissions(this Presence p, BanchoPermissions perms)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPermissions
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write((int)perms);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static void FriendList(this Presence p, int[] friendIds)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerFriendsList
            };

            using var writer = new SerializationWriter(new MemoryStream());

            if (friendIds is null)
                friendIds = Array.Empty<int>();

            writer.Write((short)friendIds.Length);

            for (var i = 0; i < friendIds?.Length; i++)
                writer.Write(friendIds[i]);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            p.PacketEnqueue(packet);
        }

        public static void UserPresenceSingle(this Presence p, int userId)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerUserPresenceSingle
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(userId);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static void ChatChannelListingComplete(this Presence p, int i)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelListingComplete
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(i);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static void ChatChannelJoinSuccess(this Presence p, string channel)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelJoinSuccess
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            writer.Write(channel);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }
        
        public static void ChatChannelAvailable(this Presence p, string name, string topic, short userCount)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelAvailable
            };
            
            using var writer = new SerializationWriter(new MemoryStream());
            
            writer.Write(name);
            writer.Write(topic);
            writer.Write(userCount);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();
            
            p.PacketEnqueue(packet);
        }

        public static void ChatMessage(this Presence p, string sender, string message, string target, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatMessage
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender);
            writer.Write(message);
            writer.Write(target);
            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void ChatMessage(this Presence p, Presence sender, string message, string target)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatMessage
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender.Username);
            writer.Write(message);
            writer.Write(target);
            writer.Write(sender.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void SpectatorJoined(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerSpectateSpectatorJoined
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void SpectatorLeft(this Presence p, int id)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerSpectateSpectatorLeft
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void NewMatch(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchNew
            };

            using var writer = new SerializationWriter(new MemoryStream());

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

        public static void MatchJoinSuccess(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchJoinSuccess
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void MatchUpdate(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchUpdate
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void MatchStart(this Presence p, Match match)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiMatchStart
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.WriteMatch(match);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void MatchPlayerFailed(this Presence p, int slotId)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiOtherFailed
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(slotId);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void MatchPlayerSkipped(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiSkipRequestOther
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(p.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void MatchTransferHost(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiHostTransfer
            };

            p.PacketEnqueue(packet);
        }

        public static void AllPlayersLoaded(this Presence p)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiAllPlayersLoaded
            };

            p.PacketEnqueue(packet);
        }

        public static void MatchInvite(this Presence p, Presence sender, string message)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerMultiInvite
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(sender.Username);
            writer.Write(message);
            writer.Write(p.Username);
            writer.Write(sender.Id);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }

        public static void RevokeChannel(this Presence p, string channel)
        {
            var packet = new Packet
            {
                Type = PacketType.ServerChatChannelRevoked
            };

            using var writer = new SerializationWriter(new MemoryStream());

            writer.Write(channel);

            packet.Data = ((MemoryStream)writer.BaseStream).ToArray();

            p.PacketEnqueue(packet);
        }
    }
}