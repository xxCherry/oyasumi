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
    public class MatchSkip
    {
        [Packet(PacketType.ClientMultiSkipRequest)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var prSlot = match.Slots.FirstOrDefault(x => x.Presence == pr);
            prSlot.Skipped = true;

            if (match.Slots.Any(slot => slot.Status == SlotStatus.Playing && !slot.Skipped))
                return;

            foreach (var presence in match.Presences)
            {
                presence.PacketEnqueue(new()
                {
                    Type = PacketType.ServerMultiSkip
                });
            }
        }
    }
}
