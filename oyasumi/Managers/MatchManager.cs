using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Objects;
using oyasumi.Layouts;
using oyasumi.Enums;

namespace oyasumi.Managers
{
    public static class MatchManager
    {
        public static Dictionary<int, Match> Matches = new ();
        private static int _idCounter;

        public static async Task JoinMatch(this Presence pr, Match match, string password)
        {
            if (pr.CurrentMatch is not null || (match.PasswordRequired && match.GamePassword != password))
            {
                pr.MatchJoinFail();
                return;
            }

            if (!Matches.TryGetValue(match.Id, out _))
            {
                match.Id = ++_idCounter;
                Matches.Add(match.Id, match);
            }

            var slot = match.FreeSlot;

            if (slot is null)
            {
                pr.MatchJoinFail();
                return;
            }
            
            slot.Status = SlotStatus.NotReady;
            slot.Presence = pr;

            match.Presences.Add(pr);

            await pr.MatchJoinSuccess(match);

            pr.CurrentMatch = match;

            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }

        public static async Task LeaveMatch(this Presence pr)
        {
            var match = pr.CurrentMatch;

            if (match is null)
                return;

            var slot = match.Slots.FirstOrDefault(x => x.Presence == pr);

            slot?.Clear();

            match.Presences.Remove(pr);
            await pr.LeaveChannel($"multi_{pr.CurrentMatch.Id}", true)
                ;

            if (match.Presences.Count == 0)
            {
                Matches.Remove(match.Id);
                ChannelManager.Channels.TryRemove($"multi_{pr.CurrentMatch.Id}", out _);
            }
            else
                foreach (var presence in match.Presences)
                    await presence.MatchUpdate(match);

            pr.CurrentMatch = null;
        }
    }
}
