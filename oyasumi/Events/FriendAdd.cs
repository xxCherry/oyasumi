using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class FriendAdd
    {
        [Packet(PacketType.ClientFriendsAdd)]
        public static void Handle(Packet p, Presence pr, OyasumiDbContext context)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var id = reader.ReadInt32();

            var friend = PresenceManager.GetPresenceById(id);

            if (friend is null) 
                return;

            var exists = context.Friends.FirstOrDefault(x => x.Friend2 == id);

            if (exists is not null)
                return;

            context.Friends.Add(new Friend
            {
                Friend1 = pr.Id,
                Friend2 = id
            });

            context.SaveChanges();
        }
    }
}
