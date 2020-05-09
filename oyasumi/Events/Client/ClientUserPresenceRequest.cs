using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace oyasumi.Events.Client
{
    public class ClientUserPresenceRequest : IPacket
    {
        public void Handle(BanchoPacket packet, Player player)
        {
            var userIdList = new BanchoIntList(packet.Data);
            foreach (var id in userIdList.Value)
            {
                var _player = Players.GetPlayerById(id);
                if (_player == default) // prob he left
                {
                    player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserQuit, new BanchoUserQuit(id)));
                }
                else
                {
                    player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresence, _player.ToUserPresence()));
                }
            }

        }
    }
}