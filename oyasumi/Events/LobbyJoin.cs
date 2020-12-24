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
    public class LobbyJoin
    {
        [Packet(PacketType.ClientLobbyJoin)]
        public static async Task Handle(Packet p, Presence pr)
        {
            foreach (var match in MatchManager.Matches.Values)
            {
                if (match.PasswordRequired)
                    match.GamePassword = " "; 
                await pr.NewMatch(match);
            }
        }
    }
}
