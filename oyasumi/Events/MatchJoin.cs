﻿using System;
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
    public class MatchJoin
    {
        [Packet(PacketType.ClientMultiMatchJoin)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var reader = new SerializationReader(new MemoryStream(p.Data));
            var matchId = reader.ReadInt32();
            var password = reader.ReadString();

            if (MatchManager.Matches.TryGetValue(matchId, out var match))
            {
                await pr.JoinMatch(match, password);
                
                if (pr.CurrentMatch is not null)
                    await pr.JoinChannel($"multi_{match.Id}");
            }
            else
                pr.MatchJoinFail();
        }
    }
}
