﻿using System;
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

            ChannelManager.SendMessage(pr, message, target, false);
        }
    }
}
