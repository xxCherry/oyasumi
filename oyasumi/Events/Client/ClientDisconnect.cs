using HOPEless.Bancho;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace oyasumi.Events.Client
{
    public class ClientDisconnect : IPacket
    {
        public void Handle(BanchoPacket packet, Player player)
        {
            if ((int)(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - player.loginTime) >= 5) // Timeout check
            {
                player.Disconnect();
            }
        }
    }
}
