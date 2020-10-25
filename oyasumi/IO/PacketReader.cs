using System;
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
		public static async IAsyncEnumerable<Packet> Parse(MemoryStream data)
		{
			using var reader = new SerializationReader(data);

			while (data.Position != data.Length)
				yield return ReadPacket(reader);
		}

		private static Packet ReadPacket(BinaryReader reader)
		{
			var type = (PacketType)reader.ReadInt16();

			/*
			if (_packetCache.TryGetValue(type, out var p))
			{
				reader.BaseStream.Seek(5, SeekOrigin.Current); // seek through bytes to set correct offset
				return p;
			} 
			*/

			reader.ReadByte();

			var length = reader.ReadInt32();
			var packetData = reader.ReadBytes(length);

			var packet = new Packet
			{
				Type = type,
				Data = packetData
			};

			/*if (length == 0)
				_packetCache.TryAdd(type, packet);*/


			return packet;
		}
		
	}
}
