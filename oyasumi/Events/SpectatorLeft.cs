using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.IO;

namespace oyasumi.Events
{
    public class SpectatorLeft
    {
        [Packet(PacketType.ClientSpectateStop)]
        public static void Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            pr.Spactaing.Spectators.Remove(pr);
            pr.Spactaing.SpectatorLeft(pr.Id);
            pr.Spactaing = null;

        }
    }
}