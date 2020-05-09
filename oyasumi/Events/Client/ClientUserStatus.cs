using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace oyasumi.Events.Client
{
    public class ClientUserStatus : IPacket
    {
        public void Handle(BanchoPacket packet, Player player)
        {
            player.Status = new BanchoUserStatus(packet.Data);
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserData, player.Status));
        }
    }
}
