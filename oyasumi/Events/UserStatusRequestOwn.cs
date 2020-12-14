using System.IO;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class UserStatusRequestOwn
    {
        [Packet(PacketType.ClientStatusRequestOwn)]
        public static async Task Handle(Packet p, Presence pr) =>
            await pr.UserStats();
    }
}