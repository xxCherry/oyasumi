using System;
using System.IO;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Cms;
using osu.Game.IO.Legacy;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Objects;
using static oyasumi.Objects.Presence;

namespace oyasumi.Events
{
    public class UserStatusUpdate
    {
        [Packet(PacketType.ClientUserStatus)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);
            
            pr.Status = new ()
            {
                Status = (ActionStatuses) reader.ReadByte(),
                StatusText = reader.ReadString(),
                BeatmapChecksum = reader.ReadString(),
                CurrentMods = (Mods) reader.ReadUInt32(),
                CurrentPlayMode = (PlayMode) reader.ReadByte(),
                BeatmapId = reader.ReadInt32()
            };

            var lbMode = pr.Status.CurrentMods switch
            {
                var mod when (mod & Mods.Relax) > 0 => LeaderboardMode.Relax,
                _ => LeaderboardMode.Vanilla,
            };

            pr.GetOrUpdateUserStats(lbMode, false);

            await pr.UserStats();
        }
    }
}