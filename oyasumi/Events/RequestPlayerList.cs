using oyasumi.Enums;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class RequestPlayerList
    {
        [Packet(PacketType.ClientRequestPlayerList)]
        public static void Handle(Packet p, Presence pr)
        {
            foreach (var presence in PresenceManager.Presences.Values)
                pr.UserPresence(presence);
        }
    }
}