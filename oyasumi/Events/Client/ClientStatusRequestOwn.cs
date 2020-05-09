using HOPEless.Bancho;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace oyasumi.Events.Client
{
    public class ClientStatusRequestOwn : IPacket
    {
        public void Handle(BanchoPacket packet, Player player)
        {
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserData, player.ToUserData()));
        }
    }
}
