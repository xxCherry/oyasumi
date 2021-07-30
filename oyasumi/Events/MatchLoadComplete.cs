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
    public class MatchLoadComplete
    {
        [Packet(PacketType.ClientMultiMatchLoadComplete)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            if (--match.NeedLoad == 0)
            {
                foreach (var presence in match.Presences)
                    presence.AllPlayersLoaded();
            }
        }
    }
}
