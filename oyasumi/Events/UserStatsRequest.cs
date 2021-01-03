using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Events
{
    public class UserStatsRequest
    {
        [Packet(PacketType.ClientUserStatsRequest)]
        public static async Task Handle(Packet p, Presence pr)
        {    
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var presenceIds = new List<int>();

            int length = reader.ReadInt16();
            for (var i = 0; i < length; i++) 
                presenceIds.Add(reader.ReadInt32());
            
            foreach (var prId in presenceIds)
            {
                if (prId == pr.Id)
                    continue;
                
                var otherPresence = PresenceManager.GetPresenceById(prId);
                if (otherPresence is not null)
                    await pr.UserStats(otherPresence);
                else
                    await pr.UserLogout(prId);
            }
        }
    }
}