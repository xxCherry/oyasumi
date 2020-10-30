using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.IO;

namespace oyasumi.Events
{
    public class SpectatorJoin
    {
        [Packet(PacketType.ClientSpectateStart)]
        public static void Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var userId = reader.ReadInt32();

            var spectatingPresence = PresenceManager.GetPresenceById(userId);

            pr.Spectating = spectatingPresence;
            pr.Spectating.SpectatorJoined(pr.Id);

            spectatingPresence.Spectators.Add(pr);
        }
    }
}