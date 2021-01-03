using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class ChatMessagePublic
    {
        [Packet(PacketType.ClientChatMessagePublic)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var client = reader.ReadString();
            var message = reader.ReadString();
            var target = reader.ReadString();

            if (target == "#spectator" && pr.Spectating is not null)
                target = $"spect_{pr.Spectating.Id}";
            else if (target == "#spectator" && pr.Spectators is not null)
                target = $"spect_{pr.Id}";
            else if (target == "#multiplayer" && pr.CurrentMatch is not null)
                target = $"multi_{pr.CurrentMatch.Id}";
                
            await ChannelManager.SendMessage(pr, message, target, true);
        }
    }
}
