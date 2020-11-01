using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Math.EC.Rfc7748;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class MatchComplete
    {
        [Packet(PacketType.ClientMultiMatchCompleted)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            match.Slots.FirstOrDefault(x => x.Presence == pr).Status = SlotStatus.Complete;

            if (match.Slots.Any(x => x.Status == SlotStatus.Playing))
                return;

            match.Unready(SlotStatus.Complete);

            match.InProgress = false;

            foreach (var presence in match.Presences)
            {
                presence.PacketEnqueue(new Packet
                {
                    Type = PacketType.ServerMultiMatchFinished
                });
                presence.MatchUpdate(match);
            }
        }
    }
}
