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
    public class MatchInvite
    {
        [Packet(PacketType.ClientMultiInvite)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var reader = new SerializationReader(new MemoryStream(p.Data));

            var target = PresenceManager.GetPresenceById(reader.ReadInt32());

            if (target is null)
                return;

            target.MatchInvite(pr, $"Come join to my game: [osump://{match.Id}/{match.GamePassword} {match.GameName}]");
        }
    }
}
