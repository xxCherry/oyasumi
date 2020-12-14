using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class MatchHasBeatmap
    {
        [Packet(PacketType.ClientMultiBeatmapAvailable)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var slot = match.Slots.FirstOrDefault(x => x.Presence == pr);

            slot.Status = SlotStatus.NotReady;

            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }
    }
}
