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
                case (byte)TypeBytes.Null:
                    return null;
                case (byte)TypeBytes.String:
                    return base.ReadString();
                default:
                    throw new Exception($"Type byte is not {TypeBytes.Null} or {TypeBytes.String} (position: {BaseStream.Position})");
            }
        }

        public Match ReadMatch(OyasumiDbContext context)
        {
            var match = new Match();

            ReadInt16(); // match id
            ReadByte(); // in progress

            match.Type = (MatchTypes)ReadByte();
            match.ActiveMods = (Mods)ReadInt32();

            match.GameName = ReadString();
            match.GamePassword = ReadString();

            ReadString(); // game name

            match.BeatmapId = ReadInt32();
            match.BeatmapChecksum = ReadString();

            match.Beatmap = BeatmapManager.Get(match.BeatmapChecksum, context).Result.Item2;

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
            switch ((TypeBytes)ReadByte())
            {
                case TypeBytes.Null:        return null;
                case TypeBytes.Bool:        return ReadBoolean();
                case TypeBytes.Byte:        return ReadByte();
                case TypeBytes.UShort:      return ReadUInt16();
                case TypeBytes.UInt:        return ReadUInt32();
                case TypeBytes.ULong:       return ReadUInt64();
                case TypeBytes.SByte:       return ReadSByte();
                case TypeBytes.Short:       return ReadInt16();
                case TypeBytes.Int:         return ReadInt32();
                case TypeBytes.Long:        return ReadInt64();
                case TypeBytes.Char:        return ReadChar();
                case TypeBytes.String:      return base.ReadString();
                case TypeBytes.Float:       return ReadSingle();
                case TypeBytes.Double:      return ReadDouble();
                case TypeBytes.Decimal:     return ReadDecimal();
                case TypeBytes.DateTime:    return ReadDateTime();
                case TypeBytes.ByteArray:   return ReadBytes();
                case TypeBytes.CharArray:   return ReadChars();
                case TypeBytes.Unknown:
                case TypeBytes.Serializable: 
                default:
                    throw new NotImplementedException();
            }
        }
    }
}