using System.Linq;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.Layouts;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Managers
{
    public class PresenceManager
    {
        public static readonly MultiKeyDictionary<int, string, string, Presence> Presences = new ();

        public static void Add(Presence p) =>
            Presences.Add(p.Id, p.Token, p.Username, p);

        public static async Task Remove(Presence p)
        {
            p.Spectating?.SpectatorLeft(p.Id);

            Presences.Remove(p.Id);

            foreach (var pr in Presences.Values)
                await pr.UserLogout(p.Id);

            foreach (var channel in p.Channels.Values)
                await p.LeaveChannel(channel.Name);
        }
        public static Presence GetPresenceByToken(string token) => Presences[token];
        public static Presence GetPresenceById(int id) => Presences[id];
        public static Presence GetPresenceByName(string name) => Presences[name, 0];
        public static void PacketEnqueue(Packet p)
        {
            foreach (var pr in Presences.Values)
                pr.PacketEnqueue(p);
        }
    }
}