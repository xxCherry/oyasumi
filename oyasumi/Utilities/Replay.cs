using System.IO;
using osu.Game.IO.Legacy;
using oyasumi.Enums;

namespace oyasumi.Utilities
{
    public class Replay
    {
        public string BeatmapChecksum { get; set; }
        public PlayMode PlayMode { get; set;}
        
        public string Username { get; set;}
        
        public ushort Count300 { get; set; }
        public ushort Count100 { get; set; }
        public ushort Count50 { get; set; }
        public ushort CountGeki { get; set; }
        public ushort CountKatu { get; set; }
        public ushort CountMiss { get; set; }
        public int TotalScore { get; set; }
        public int MaxCombo { get; set; }
        public Mods Mods { get; set; }

        public static Replay Parse(Stream stream)
        {
            using var sr = new SerializationReader(stream);
            var replay = new Replay();
            
            replay.PlayMode = (PlayMode) sr.ReadByte();
            sr.ReadInt32(); // Version

            replay.BeatmapChecksum = sr.ReadString();
            replay.Username = sr.ReadString();

            sr.ReadString(); // Replay Checksum

            replay.Count300 = sr.ReadUInt16();
            replay.Count100 = sr.ReadUInt16();
            replay.Count50 = sr.ReadUInt16();
            replay.CountGeki = sr.ReadUInt16();
            replay.CountKatu = sr.ReadUInt16();
            replay.CountMiss = sr.ReadUInt16();

            replay.TotalScore = sr.ReadInt32();
            replay.MaxCombo = sr.ReadUInt16();

            sr.ReadBoolean(); // Perfect
            
            replay.Mods = (Mods)sr.ReadInt32();

            sr.ReadString(); // HpGraph
            sr.ReadInt64(); // Date
            sr.ReadByteArray(); // Replay Data

            sr.ReadInt64(); // OnlineID, i guess we don't need check 2012-2014 clients
            
            // TODO: replay data parsing (i do it when will make anticheat)
            
            return replay;
        }
    }
}