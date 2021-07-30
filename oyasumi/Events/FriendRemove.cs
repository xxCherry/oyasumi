using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class FriendRemove
    {
        [Packet(PacketType.ClientFriendsRemove)]
        public static void Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var id = reader.ReadInt32();

            var friend = DbContext.Friends.FirstOrDefault(x => x.Friend2 == id);

            if (friend is null) 
                return;
            
            DbContext.Friends.Remove(friend);

            Base.FriendCache.FirstOrDefault(x => x.Key == friend.Friend1).Value.Remove(friend.Friend2);
        }
    }
}
