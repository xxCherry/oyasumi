using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.Collections.Generic;
using System.IO;

namespace oyasumi.Events
{
    public class SpectatorFrames
    {
        [Packet(PacketType.ClientSpectateData)]
        public static void Handle(Packet p, Presence pr)
        {
            foreach (var presence in pr.Spectators)
            {
                presence.PacketEnqueue(new Packet() 
                { 
                    Type = PacketType.ServerSpectateData,
                    Data = p.Data 
                });
            }
        }
    }
}