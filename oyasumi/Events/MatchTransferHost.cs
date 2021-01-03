using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class MatchTransferHost
    {
        [Packet(PacketType.ClientMultiTransferHost)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var reader = new SerializationReader(new MemoryStream(p.Data));

            var slotIndex = reader.ReadInt32();

            if (match.InProgress || slotIndex > Match.MAX_PLAYERS || slotIndex < 0)
                return;

            var newHost = match.Slots[slotIndex];

            if (newHost.Presence is null)
                return;

            match.Host = newHost.Presence;
            await match.Host.MatchTransferHost();
            
            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }
    }
}
