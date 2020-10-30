﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using oyasumi.Extensions;
using oyasumi.Objects;

namespace oyasumi.IO
{
	public class PacketWriter
	{
		private readonly MemoryStream _data;
		private bool _clean;

        public PacketWriter() =>
			_data = new MemoryStream();

		public byte[] ToBytes()
		{
			if (_clean)
				throw new Exception("Unable to access because ToBytes() method can be called only once");
			
			var array = _data.ToArray();
			
			_clean = true;

			return array;
		}

		public async Task Write(IAsyncEnumerable<Packet> packetList)
		{
			await foreach (var packet in packetList)
				await Write(packet);
		}

		public async Task Write(Packet packet)
		{
			await using var ms = new MemoryStream();
			await using var writer = new SerializationWriter(ms);
			
			writer.Write((short) packet.Type);
			writer.Write((byte) 0);
			writer.Write(packet.Data);
			
			ms.WriteTo(_data);
		}
	}
}
