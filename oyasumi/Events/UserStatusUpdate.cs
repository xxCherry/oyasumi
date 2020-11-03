using System;
using System.IO;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class UserStatusUpdate
    {
        [Packet(PacketType.ClientUserStatus)]
        public static async void Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);
            
            pr.Status = new PresenceStatus
            {
                Status = (ActionStatuses) reader.ReadByte(),
                StatusText = reader.ReadString(),
                BeatmapChecksum = reader.ReadString(),
                CurrentMods = (Mods) reader.ReadUInt32(),
                CurrentPlayMode = (PlayMode)reader.ReadByte(),
                BeatmapId = reader.ReadInt32()
            };

            // We just want to update our stats from cache, so we don't
            // use context here
            await pr.GetOrUpdateUserStats(null, false); 
            
            pr.UserStats();
        }
    }
}