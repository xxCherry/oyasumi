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
    public class MatchFailed
    {
        [Packet(PacketType.ClientMultiFailed)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var slotId = -1;
            for (int i = 0; i < Match.MAX_PLAYERS; i++)
            {
                if (match.Slots[i].Presence == pr)
                {
                    slotId = i;
                    break;
                }
            }

            foreach (var presence in match.Presences)
                await presence.MatchPlayerFailed(slotId);
        }
    }
}
