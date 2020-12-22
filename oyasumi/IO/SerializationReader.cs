using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.IO
{
    public class SerializationReader : BinaryReader
    {
        public SerializationReader(Stream input) : base(input) { }
        public SerializationReader(Stream input, Encoding encoding) : base(input, encoding) { }

        public byte[] ReadBytes()   // an overload to ReadBytes(int count)
        {
            int length = ReadInt32();
            return length > 0
                ? base.ReadBytes(length)
                : null;
        }

        public char[] ReadChars()   // an overload to ReadChars(int count)
        {
            int length = ReadInt32();
            return length > 0
                ? base.ReadChars(length)
                : null;
        }

        public override string ReadString()
        {
            switch (ReadByte())
            {
                case (byte)ByteTypes.Null:
                    return null;
                case (byte)ByteTypes.String:
                    return base.ReadString();
                default:
                    throw new Exception($"Type byte is not {ByteTypes.Null} or {ByteTypes.String} (position: {BaseStream.Position})");
            }
        }

        public Match ReadMatch()
        {
            var match = new Match();

            ReadInt16(); // match id
            ReadByte(); // in progress

            match.Type = (MatchTypes)ReadByte();
            match.ActiveMods = (Mods)ReadInt32();

            match.GameName = ReadString();
            match.GamePassword = ReadString();

            ReadString(); // beatmap name

            match.BeatmapId = ReadInt32();
            match.BeatmapChecksum = ReadString();

            BeatmapManager.Get(match.BeatmapChecksum, "", 0)
                .ContinueWith(x => match.Beatmap = x.Result.Item2)
                .Wait();

            foreach (var slot in match.Slots)
                slot.Status = (SlotStatus)ReadByte();

            foreach (var slot in match.Slots)
                slot.Team = (SlotTeams)ReadByte();

            foreach (var slot in match.Slots)
                if ((slot.Status & SlotStatus.HasPlayer) > 0)
                    ReadInt32();

            match.Host = PresenceManager.GetPresenceById(ReadInt32());

            match.PlayMode = (PlayMode)ReadByte();
            match.ScoringType = (MatchScoringTypes)ReadByte();
            match.TeamType = (MatchTeamTypes)ReadByte();
            match.FreeMods = ReadBoolean();
            
            if (match.FreeMods)
                foreach (var slot in match.Slots)
                    slot.Mods = (Mods)ReadInt32();

            match.Seed = ReadInt32();

            return match;
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64(), DateTimeKind.Utc);
        }

        public List<T> ReadList<T>()
        {
            var l = new List<T>();
            int length = ReadInt32();
            for (int i = 0; i < length; i++)
                l.Add((T)ReadObject());
            return l;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            var dic = new Dictionary<TKey, TValue>();
            int length = ReadInt32();
            for (int i = 0; i < length; i++)
                dic[(TKey) ReadObject()] = (TValue) ReadObject();
            return dic;
        }

        public object ReadObject()
        {
            switch ((ByteTypes)ReadByte())
            {
                case ByteTypes.Null:        return null;
                case ByteTypes.Bool:        return ReadBoolean();
                case ByteTypes.Byte:        return ReadByte();
                case ByteTypes.UShort:      return ReadUInt16();
                case ByteTypes.UInt:        return ReadUInt32();
                case ByteTypes.ULong:       return ReadUInt64();
                case ByteTypes.SByte:       return ReadSByte();
                case ByteTypes.Short:       return ReadInt16();
                case ByteTypes.Int:         return ReadInt32();
                case ByteTypes.Long:        return ReadInt64();
                case ByteTypes.Char:        return ReadChar();
                case ByteTypes.String:      return base.ReadString();
                case ByteTypes.Float:       return ReadSingle();
                case ByteTypes.Double:      return ReadDouble();
                case ByteTypes.Decimal:     return ReadDecimal();
                case ByteTypes.DateTime:    return ReadDateTime();
                case ByteTypes.ByteArray:   return ReadBytes();
                case ByteTypes.CharArray:   return ReadChars();
                case ByteTypes.Unknown:
                case ByteTypes.Serializable: 
                default:
                    throw new NotImplementedException();
            }
        }
    }
}