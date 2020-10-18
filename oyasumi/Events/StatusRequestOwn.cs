using System.IO;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class StatusRequestOwn
    {
        [Packet(PacketType.ClientStatusRequestOwn)]
        public static void Handle(Packet p, Presence pr) =>
            pr.UserStats();
    }
}