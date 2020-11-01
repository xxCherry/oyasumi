using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;


namespace oyasumi.Events
{
    public class ChatMessagePrivate
    {
        [Packet(PacketType.ClientChatMessagePrivate)]
        public static void Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var client = reader.ReadString();
            var message = reader.ReadString();
            var target = reader.ReadString();

            if (target == "oyasumi") // Handle commands if bot
            {
                if (message.StartsWith("!"))
                {   
                    var command = message[1..]; // from 1 to end of string
                    if (command == "ping")
                    {
                        ChannelManager.SendMessage("oyasumi", "Pong", pr.Username, 1, false);
                    }
                }
            }
            else
            {
                ChannelManager.SendMessage(pr, message, target, false);
            }
        }
    }
}
