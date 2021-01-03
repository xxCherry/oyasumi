using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Math.EC;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class MatchChangeOption
    {
        [Packet(PacketType.ClientMultiSettingsChange)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var reader = new SerializationReader(new MemoryStream(p.Data));
            var newMatch = await reader.ReadMatch();

            var match = pr.CurrentMatch;

            if (match is null)
                return;

            
            if (match.BeatmapChecksum != newMatch.BeatmapChecksum ||
                match.PlayMode != newMatch.PlayMode               ||
                match.Type != newMatch.Type                       ||
                match.ScoringType != newMatch.ScoringType         ||
                match.TeamType != newMatch.TeamType)
            {
                match.Unready(SlotStatus.Ready);
            }
            
            match.Beatmap = newMatch.Beatmap;
            match.BeatmapChecksum = newMatch.BeatmapChecksum;
            match.BeatmapId = newMatch.BeatmapId;
            match.SpecialModes = newMatch.SpecialModes;
            match.GameName = newMatch.GameName.Length > 0 ? newMatch.GameName : $"{match.Host.Username}'s game";

            if (match.TeamType != newMatch.TeamType)
            {
                if (match.TeamType == MatchTeamTypes.TagTeamVs || match.TeamType == MatchTeamTypes.TeamVs)
                {
                    var i = 0;
                    foreach (var slot in match.Slots)
                    {
                        if (slot.Team == SlotTeams.Neutral)
                            slot.Team = i % 2 == 1 ? SlotTeams.Red : SlotTeams.Blue;
                        i++;
                    }
                }
                else
                {
                    foreach (var slot in match.Slots)
                        slot.Team = SlotTeams.Neutral;
                }
            }

            match.Type = newMatch.Type;
            match.ScoringType = newMatch.ScoringType;
            match.TeamType = newMatch.TeamType;
            match.PlayMode = newMatch.PlayMode;
            match.Seed = newMatch.Seed;

            if (match.TeamType == MatchTeamTypes.TagCoop)
                match.SpecialModes &= ~MultiSpecialModes.FreeMod;

            if (newMatch.FreeMods != match.FreeMods)
            {
                if (newMatch.FreeMods)
                {
                    foreach (var slot in match.Slots)
                    {
                        if ((slot.Status & SlotStatus.HasPlayer) > 0)
                            slot.Mods = match.ActiveMods & ~Mods.SpeedAltering;
                    }
                    match.ActiveMods &= Mods.SpeedAltering;
                }
                else
                {
                    foreach (var slot in match.Slots)
                    {
                        if (slot.Presence is not null && slot.Presence.Id == match.Host.Id)
                        {
                            match.ActiveMods = slot.Mods | (match.ActiveMods & Mods.SpeedAltering);
                            break;
                        }
                    }
                }
            }

            match.FreeMods = newMatch.FreeMods;

            foreach (var presence in match.Presences)
                await presence.MatchUpdate(match);
        }
    }
}
