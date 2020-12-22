using oyasumi.Enums;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace oyasumi.IO 
{
    public class SerializationWriter : BinaryWriter
    {
        public SerializationWriter(Stream output) : base(output)
        {
        }

        public SerializationWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public override void Write(string str)
        {
            if (str == null)
                Write((byte) ByteTypes.Null);
            else
            {
                Write((byte) ByteTypes.String);
                base.Write(str);
            }
        }

        public override void Write(byte[] bytes)
        {
            if (bytes == null)
                Write(0);
            else
            {
                Write(bytes.Length);
                if (bytes.Length > 0)
                    base.Write(bytes);
            }
        }

        public void WriteRaw(byte[] bytes)
        {
            base.Write(bytes);
        }

        public override void Write(char[] chars)
        {
            if (chars == null)
                Write(-1);
            else
            {
                Write(chars.Length);
                if (chars.Length > 0)
                    base.Write(chars);
            }
        }

        public void WriteRaw(char[] chars)
        {
            base.Write(chars);
        }

        public void Write(DateTime time)
        {
            Write(time.ToUniversalTime().Ticks);
        }

        public void Write<T>(List<T> list)
        {
            if (list == null)
                Write(-1);
            else
            {
                Write(list.Count);
                foreach (T t in list)
                    WriteObject(t);
            }
        }

        public void Write<TKey, TValue>(IDictionary<TKey, TValue> dic)
        {
            if (dic == null)
                Write(-1);
            else
            {
                Write(dic.Count);
                foreach (KeyValuePair<TKey, TValue> kvp in dic)
                {
                    WriteObject(kvp.Key);
                    WriteObject(kvp.Value);
                }
            }
        }

        public void WriteMatch(Match match)
        {
            Write((short)match.Id);
            Write(match.InProgress);
            Write((byte)match.Type);
            Write((uint)match.ActiveMods);
            Write(match.GameName);
            Write(match.GamePassword);
            Write(match.Beatmap.BeatmapName);
            Write(match.BeatmapId);
            Write(match.BeatmapChecksum);

            for (int i = 0; i < Match.MAX_PLAYERS; i++)
                Write((byte)match.Slots[i].Status);

            for (int i = 0; i < Match.MAX_PLAYERS; i++)
                Write((byte)match.Slots[i].Team);

            for (int i = 0; i < Match.MAX_PLAYERS; i++)
                if ((match.Slots[i].Status & SlotStatus.HasPlayer) > 0)
                    Write(match.Slots[i].Presence.Id);

            Write(match.Host.Id);

            Write((byte)match.PlayMode);
            Write((byte)match.ScoringType);
            Write((byte)match.TeamType);
            Write(match.FreeMods);

            //Write((byte)match.SpecialModes);

            if (match.FreeMods)
                for (int i = 0; i < Match.MAX_PLAYERS; i++)
                    Write((int)match.Slots[i].Mods);

            Write(match.Seed);
        }

        private void WriteObject(object o)
        {
            if (o == null)
                Write((byte) ByteTypes.Null);
            else
                switch (o)
                {
                    case bool v:
                        Write((byte) ByteTypes.Bool);
                        base.Write(v);
                        break;
                    case byte v:
                        Write((byte) ByteTypes.Byte);
                        base.Write(v);
                        break;
                    case ushort v:
                        Write((byte) ByteTypes.UShort);
                        base.Write(v);
                        break;
                    case uint v:
                        Write((byte) ByteTypes.UInt);
                        base.Write(v);
                        break;
                    case ulong v:
                        Write((byte) ByteTypes.ULong);
                        base.Write(v);
                        break;
                    case sbyte v:
                        Write((byte) ByteTypes.SByte);
                        base.Write(v);
                        break;
                    case short v:
                        Write((byte) ByteTypes.Short);
                        base.Write(v);
                        break;
                    case int v:
                        Write((byte) ByteTypes.Int);
                        base.Write(v);
                        break;
                    case long v:
                        Write((byte) ByteTypes.Long);
                        base.Write(v);
                        break;
                    case char v:
                        Write((byte) ByteTypes.Char);
                        base.Write(v);
                        break;
                    case string v:
                        Write((byte) ByteTypes.String);
                        base.Write(v);
                        break;
                    case float v:
                        Write((byte) ByteTypes.Float);
                        base.Write(v);
                        break;
                    case double v:
                        Write((byte) ByteTypes.Double);
                        base.Write(v);
                        break;
                    case decimal v:
                        Write((byte) ByteTypes.Decimal);
                        base.Write(v);
                        break;
                    case DateTime v:
                        Write((byte) ByteTypes.DateTime);
                        Write(v);
                        break;
                    case byte[] v:
                        Write((byte) ByteTypes.ByteArray);
                        base.Write(v);
                        break;
                    case char[] v:
                        Write((byte) ByteTypes.CharArray);
                        base.Write(v);
                        break;
                    default:
                        throw new NotImplementedException();
                }
        }
    }
}

