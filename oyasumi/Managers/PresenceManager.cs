using System.Linq;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Managers
{
    public class PresenceManager
    {
        public static readonly TwoKeyDictionary<int, string, Presence> Presences = new TwoKeyDictionary<int, string, Presence>();
        
        public static void Add(Presence p) => Presences.Add(p.Id, p.Token, p);
        public static void Remove(Presence p) =>  Presences.Remove(p.Id);
        public static Presence GetPresenceByToken(string token) => Presences[token];
        public static Presence GetPresenceById(int id) => Presences[id];
        public static Presence GetPresenceByName(string name) => Presences.Values.FirstOrDefault(x => x.Username == name);

        public static void PacketEnqueue(Packet p)
        {
            foreach (var pr in Presences.Values)
                pr.PacketEnqueue(p);
        }
    }
}