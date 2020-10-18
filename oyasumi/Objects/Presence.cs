using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Utilities;

namespace oyasumi.Objects
{
	public class Presence
	{
		public readonly string Token;
		public readonly string Username;
		
		public readonly int Id;
		public readonly int LoginTime = Time.CurrentUnixTimestamp;

		// --- User Presence
		public byte Timezone = 0;
		public float Longitude = 0;
		public float Latitude = 0;
		public byte CountryCode = 1;
		// --- > Shared
		public int Rank = 1;
		// --- User Stats
		public PresenceStatus Status;
		public long RankedScore = 0;
		public float Accuracy = 0;
		public int PlayCount = 0;
		public long TotalScore = 0;
		public short Performance = 0;
		// --

		public int LastPing = 0;
		
		private readonly ConcurrentQueue<Packet> _packetQueue = new ConcurrentQueue<Packet>();

		public Presence(int id, string username)
		{
			Id = id;
			Username = username;
			Token = Guid.NewGuid().ToString();
			Status = new PresenceStatus
			{
				Status = ActionStatuses.Unknown,
				StatusText = "",
				BeatmapChecksum = "",
				BeatmapId = 0,
				CurrentMods = Mods.None,
				CurrentPlayMode = PlayMode.Osu
			};
			this.UserStats();
		}

		public void PacketEnqueue(Packet p)
		{
			_packetQueue.Enqueue(p);
		}
		
		public async Task<byte[]> WritePackets()
		{
			var writer = new PacketWriter();
			while (_packetQueue.TryDequeue(out var p))
			{
				await writer.Write(p);
			}

			return writer.ToBytes();
		}
	}
}
