using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.Collections.Generic;
using System.IO;

namespace oyasumi.Events
{
    public class MatchScoreUpdate
    {
        [Packet(PacketType.ClientMultiScoreUpdate)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var slotIndex = -1;
            for (var i = 0; i < Match.MAX_PLAYERS; i++)
            {
                if (match.Slots[i].Presence == pr)
                {
                    slotIndex = i;
                    break;
                }
            }
            
            p.Data[4] = (byte)slotIndex;

            foreach (var presence in pr.CurrentMatch.Presences)
            {
                presence.PacketEnqueue(new ()
                {
                    Type = PacketType.ServerMultiScoreUpdate,
                    Data = p.Data
                });
            }
        }
    }
}