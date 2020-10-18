using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Events
{
    public class Disconnect
    {
        [Packet(PacketType.ClientDisconnect)]
        public static void Handle(Packet p, Presence pr)
        {
            if (Time.CurrentUnixTimestamp - pr.LoginTime < 5)
                return;
            
            PresenceManager.Remove(pr);
        }
    }
}