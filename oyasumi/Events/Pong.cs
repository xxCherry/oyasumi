using System;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.Layouts;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Events
{
    public class Pong
    {
        [Packet(PacketType.ClientPong)]
        public static async Task Handle(Packet p, Presence pr)
        {
        }
    }
}