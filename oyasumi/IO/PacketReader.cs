using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.IO
{
	public class PacketReader
	{
		public static IEnumerable<Packet> Parse(Stream data)
		{
			using var reader = new SerializationReader(data);
			while (data.Position != data.Length)
				yield return ReadPacket(reader);
		}

		private static Packet ReadPacket(BinaryReader reader)
		{
			var type = (PacketType)reader.ReadInt16();
			reader.ReadByte();

			var length = reader.ReadInt32();
			var packetData = reader.ReadBytes(length);

			return new Packet
			{
				Type = type,
				Data = packetData
			};
		}
		
	}
}
