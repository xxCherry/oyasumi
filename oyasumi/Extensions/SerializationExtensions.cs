using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Extensions
{
    public static class SerializationExtensions
    {
        public static async Task<Match> ReadMatch(this SerializationReader reader)
        {
            var match = new Match();

            reader.ReadInt16(); // match id
            reader.ReadByte(); // in progress

            match.Type = (MatchTypes)reader.ReadByte();
            match.ActiveMods = (Mods)reader.ReadInt32();

            match.GameName = reader.ReadString();
            match.GamePassword = reader.ReadString();

            reader.ReadString(); // beatmap name

            match.BeatmapId = reader.ReadInt32();
            match.BeatmapChecksum = reader.ReadString();

            match.Beatmap = (await BeatmapManager.Get(match.BeatmapChecksum)).Item2;

            foreach (var slot in match.Slots)
                slot.Status = (SlotStatus)reader.ReadByte();

            foreach (var slot in match.Slots)
                slot.Team = (SlotTeams)reader.ReadByte();

            foreach (var slot in match.Slots)
                if ((slot.Status & SlotStatus.HasPlayer) > 0)
                    reader.ReadInt32();

            match.Host = PresenceManager.GetPresenceById(reader.ReadInt32());

            match.PlayMode = (PlayMode)reader.ReadByte();
            match.ScoringType = (MatchScoringTypes)reader.ReadByte();
            match.TeamType = (MatchTeamTypes)reader.ReadByte();
            match.FreeMods = reader.ReadBoolean();
            
            if (match.FreeMods)
                foreach (var slot in match.Slots)
                    slot.Mods = (Mods)reader.ReadInt32();

            match.Seed = reader.ReadInt32();

            return match;
        }
        
        public static void WriteMatch(this SerializationWriter writer, Match match)
        {
            writer.Write((short)match.Id);
            writer.Write(match.InProgress);
            writer.Write((byte)match.Type);
            writer.Write((uint)match.ActiveMods);
            writer.Write(match.GameName);
            writer.Write(match.GamePassword);
            writer.Write(match.Beatmap.BeatmapName);
            writer.Write(match.BeatmapId);
            writer.Write(match.BeatmapChecksum);

            for (var i = 0; i < Match.MAX_PLAYERS; i++)
                writer.Write((byte)match.Slots[i].Status);

            for (var i = 0; i < Match.MAX_PLAYERS; i++)
                writer.Write((byte)match.Slots[i].Team);

            for (var i = 0; i < Match.MAX_PLAYERS; i++)
                if ((match.Slots[i].Status & SlotStatus.HasPlayer) > 0)
                    writer.Write(match.Slots[i].Presence.Id);

            writer.Write(match.Host.Id);

            writer.Write((byte)match.PlayMode);
            writer.Write((byte)match.ScoringType);
            writer.Write((byte)match.TeamType);
            writer.Write(match.FreeMods);

            //Write((byte)match.SpecialModes);

            if (match.FreeMods)
                for (var i = 0; i < Match.MAX_PLAYERS; i++)
                    writer.Write((int)match.Slots[i].Mods);

            writer.Write(match.Seed);
        }
    }
}