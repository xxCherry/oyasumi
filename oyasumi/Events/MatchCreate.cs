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
        public static async Task Handle(Packet p, Presence pr)
        {
            var reader = new SerializationReader(new MemoryStream(p.Data));
            var match = reader.ReadMatch();

            if (string.IsNullOrEmpty(match.GamePassword))
                match.GamePassword = null;

            match.Host = pr;

            await pr.JoinMatch(match, match.GamePassword);

            var channel = new Channel($"multi_{match.Id}", "", 1, true);
            match.Channel = channel;
            ChannelManager.Channels.TryAdd(channel.RawName, channel);

            await pr.JoinChannel($"multi_{match.Id}");
        }
    }
}
