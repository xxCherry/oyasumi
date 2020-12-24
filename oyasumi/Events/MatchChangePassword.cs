using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class MatchChangePassword
    {
        [Packet(PacketType.ClientMultiChangePassword)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var reader = new SerializationReader(new MemoryStream(p.Data));

            var newMatch = await reader.ReadMatch();

            if (string.IsNullOrEmpty(newMatch.GamePassword))
                match.GamePassword = null;

            match.GamePassword = newMatch.GamePassword;

            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }
    }
}
