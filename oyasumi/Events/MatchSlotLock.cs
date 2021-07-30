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
    public class MatchSlotLock
    {
        [Packet(PacketType.ClientMultiSlotLock)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var reader = new SerializationReader(new MemoryStream(p.Data));
            var slotIndex = reader.ReadInt32();

            if (match.InProgress || slotIndex > Match.MAX_PLAYERS - 1 || slotIndex < 0)
                return;

            var slot = match.Slots[slotIndex];

            if (slot.Presence == match.Host)
                return;

            if (slot.Status == SlotStatus.Locked)
                slot.Status = SlotStatus.Open;
            else if ((slot.Status & SlotStatus.HasPlayer) > 0)
            {
                slot.Mods = Mods.None;
                slot.Presence = null;
                slot.Status = SlotStatus.Locked;
                slot.Team = SlotTeams.Neutral;
            }
            else
                slot.Status = SlotStatus.Locked;

            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }
    }
}
