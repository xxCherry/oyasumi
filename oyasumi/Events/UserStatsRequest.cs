using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static void Handle(Packet p, Presence pr)
        {    
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var presenceIds = new List<int>();
            int length = reader.ReadInt16();
            for (var i = 0; i < length; i++) 
                presenceIds.Add(reader.ReadInt32());

            foreach (var prId in presenceIds)
            {
                var otherPresence = PresenceManager.GetPresenceById(prId);
                if (otherPresence is not null)
                    pr.UserStats(otherPresence);
                else
                    pr.UserLogout(prId);
            }
        }
    }
}