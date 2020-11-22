using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.Collections.Generic;
using System.IO;

namespace oyasumi.Events
{
    public class SpectatorCantSpectate
    {
        [Packet(PacketType.ServerSpectateNoBeatmap)]
        public static void Handle(Packet p, Presence pr)
        {
            var cantSpectatePacket = new Packet()
            {
                Type = PacketType.ServerSpectateNoBeatmap,
                Data = p.Data
            };

            pr.Spectating.PacketEnqueue(cantSpectatePacket);

            foreach (var presence in pr.Spectators)
            {
                presence.PacketEnqueue(cantSpectatePacket);
            }
        }
    }
}