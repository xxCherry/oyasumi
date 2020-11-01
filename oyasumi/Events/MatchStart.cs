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
    public class MatchStart
    {
        [Packet(PacketType.ClientMultiMatchStart)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            match.Start();
        }
    }
}
