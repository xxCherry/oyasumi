using System;
using System.Collections;
using System.Collections.Concurrent;
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
		private static readonly ConcurrentDictionary<PacketType, Packet> _packetCache = new ConcurrentDictionary<PacketType, Packet>();
		public static List<Packet> Parse(MemoryStream data)
		{
			using var reader = new SerializationReader(data);

			var packets = new List<Packet>();

			while (data.Position != data.Length)
				packets.Add(Read(reader));

			return packets;
		}

		private static Packet Read(BinaryReader reader)
		{
			var type = (PacketType)reader.ReadInt16();

			reader.ReadByte();

			var length = reader.ReadInt32();
			var packetData = reader.ReadBytes(length);

			var packet = new Packet
			{
				Type = type,
				Data = packetData
			};

			return packet;
		}
		
	}
}
