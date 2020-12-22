using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class ChatChannelLeave
    {
        [Packet(PacketType.ClientChatChannelLeave)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var channel = reader.ReadString();

            await pr.LeaveChannel(channel);
        }
    }
}
