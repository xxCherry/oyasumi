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

namespace oyasumi.Events
{
    public class MatchUpdateMods
    {
        [Packet(PacketType.ClientMultiChangeMods)]
        public static void Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var reader = new SerializationReader(new MemoryStream(p.Data));

            var mods = (Mods)reader.ReadInt32();

            if (match.FreeMods)
            {
                if (match.Host == pr)
                    match.ActiveMods = mods & Mods.SpeedAltering;

                match.Slots.FirstOrDefault(x => x.Presence == pr).Mods = mods & ~Mods.SpeedAltering;
            }
            else
                match.ActiveMods = mods;

            foreach (var presence in match.Presences)
                presence.MatchUpdate(match);
        }
    }
}
