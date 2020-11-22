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
using oyasumi.Database;

namespace oyasumi.Events
{
    public class MatchCreate
    {
        [Packet(PacketType.ClientMultiMatchCreate)]
        public static void Handle(Packet p, Presence pr, OyasumiDbContext context)
        {
            var reader = new SerializationReader(new MemoryStream(p.Data));
            var match = reader.ReadMatch(context);

            if (string.IsNullOrEmpty(match.GamePassword))
                match.GamePassword = null;

            match.Host = pr;

            MatchManager.JoinMatch(pr, match, match.GamePassword);

            var channel = new Channel($"multi_{match.Id}", "", 1, true);
            match.Channel = channel;
            ChannelManager.Channels.TryAdd(channel.RawName, channel);

            pr.JoinChannel($"multi_{match.Id}");
        }
    }
}
