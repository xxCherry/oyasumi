using System;
using oyasumi.Enums;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Events
{
    public class Pong
    {
        [Packet(PacketType.ClientPong)]
        public static void Handle(Packet p, Presence pr)
        {
            //pr.LastPing = Time.CurrentUnixTimestamp;
        }
    }
}