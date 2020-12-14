using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.Threading.Tasks;

namespace oyasumi.Events
{
    public class Disconnect
    {
        [Packet(PacketType.ClientDisconnect)]
        public static async Task Handle(Packet p, Presence pr)
        {
            if (Time.CurrentUnixTimestamp - pr.LoginTime < 2)
                return;
            
            await PresenceManager.Remove(pr);
        }
    }
}