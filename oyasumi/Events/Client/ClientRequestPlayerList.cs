using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace oyasumi.Events.Client
{
    public class ClientRequestPlayerList : IPacket
    {
        public void Handle(BanchoPacket packet, Player player)
        {
            foreach (Player p in Players.PlayerList)
            {
                player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresence, p.ToUserPresence()));
            }
        }
    }
}
