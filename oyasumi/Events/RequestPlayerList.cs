using oyasumi.Enums;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using System.Threading.Tasks;

namespace oyasumi.Events
{
    public class RequestPlayerList
    {
        [Packet(PacketType.ClientRequestPlayerList)]
        public static async Task Handle(Packet p, Presence pr)
        {
            foreach (var presence in PresenceManager.Presences.Values)
                await pr.UserPresence(presence);
        }
    }
}