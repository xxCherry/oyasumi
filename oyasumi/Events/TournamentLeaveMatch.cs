using System.IO;
using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class TournamentLeaveMatch
    {
        [Packet(PacketType.ClientMultiLeaveChannel)]
        public static async Task Handle(Packet p, Presence pr)
        {
            if (!pr.Tourney && (pr.BanchoPermissions & BanchoPermissions.Tournament) == 0)
                return;
            
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var matchId = reader.ReadInt32();
            
            MatchManager.Matches.TryGetValue(matchId, out var match);

            if (match is not null)
                await pr.LeaveChannel("multi_" + match.Id);
        }
    }
}