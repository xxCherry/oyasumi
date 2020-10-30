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
            if (pr.Spectating is not null)
            {
                pr.Spectating.Spectators.Remove(pr);
                pr.Spectating.SpectatorLeft(pr.Id);
                pr.Spectating = null;
            }
        }
    }
}