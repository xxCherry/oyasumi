﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Game.IO.Legacy;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.IO
{
	public static class PacketReader
	{
		private static readonly ConcurrentDictionary<PacketType, Packet> _packetCache = new ();
		public static IEnumerable<Packet> Parse(MemoryStream data)
		{
			using var reader = new SerializationReader(data);

			while (data.Position != data.Length)
				yield return Read(reader);
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
