using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Utilities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace oyasumi.Objects
{
	public class Presence
	{
		public readonly string Token;
		public readonly string Username;

		public readonly User User;

		public Match CurrentMatch;

		public Presence Spectating;
		public List<Presence> Spectators = new List<Presence>();

		public List<Channel> Channels = new List<Channel>();

		public readonly int Id;
		public readonly int LoginTime = Time.CurrentUnixTimestamp;
		public Privileges Privileges;

		// --- User Presence
		public byte Timezone;
		public float Longitude;
		public float Latitude;
		public byte CountryCode = 1;
		// --- > Shared
		public int Rank;
		// --- User Stats
		public PresenceStatus Status;
		public long RankedScore;
        public float Accuracy;
		public int PlayCount;
		public long TotalScore;
		public short Performance;
		// --

		public int LastPing;

        private readonly ConcurrentQueue<Packet> _packetQueue = new ConcurrentQueue<Packet>();

		public Presence(User user)
		{
			Id = user.Id;
			Username = user.Username;
			Token = Guid.NewGuid().ToString();
			Privileges = Privileges.Normal | Privileges.Verified;
			Status = new PresenceStatus
			{
				Status = ActionStatuses.Idle,
				StatusText = "",
				BeatmapChecksum = "",
				BeatmapId = 0,
				CurrentMods = Mods.None,
				CurrentPlayMode = PlayMode.Osu
			};

			User = user;
		}

		public Presence(int id, string username, long rankedScore, float accuracy, int playCount, long totalScore, short performance, int rank)
		{
			Id = id;
			Username = username;
			Token = Guid.NewGuid().ToString();
			Privileges = Privileges.Normal | Privileges.Verified;
			Status = new PresenceStatus
			{
				Status = ActionStatuses.Idle,
				StatusText = "",
				BeatmapChecksum = "",
				BeatmapId = 0,
				CurrentMods = Mods.None,
				CurrentPlayMode = PlayMode.Osu
			};

			RankedScore = rankedScore;
			Accuracy = accuracy;
			PlayCount = playCount;
			TotalScore = totalScore;
			Performance = performance;
			Rank = rank;
		}

		public async Task GetOrUpdateUserStats(OyasumiDbContext context, bool update)
		{
			long rankedScore = 0;
			long totalScore = 0;
			int performance = 0;
			float accuracy = 0.0f;
			int playCount = 0;

			var exists = Base.UserStatsCache.TryGetValue(User.Id, out var cachedStats);

			UserStats stats = null;

			if (!update && exists) 
				stats = cachedStats; 
			else
			{
				stats = await context.UsersStats.AsNoTracking().AsAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == User.Id);

				if (!exists)
					Base.UserStatsCache.TryAdd(User.Id, stats);

				Base.UserStatsCache.TryUpdate(User.Id, stats, cachedStats);
			}

			performance = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.PerformanceOsu,
				PlayMode.Taiko => stats.PerformanceTaiko,
				PlayMode.CatchTheBeat => stats.PerformanceCtb,
				PlayMode.OsuMania => stats.PerformanceMania,
				_ => 0
			};

			if (performance > short.MaxValue)
				performance = 0;

			totalScore = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.TotalScoreOsu,
				PlayMode.Taiko => stats.TotalScoreTaiko,
				PlayMode.CatchTheBeat => stats.TotalScoreCtb,
				PlayMode.OsuMania => stats.TotalScoreMania,
				_ => 0
			};

			rankedScore = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.RankedScoreOsu,
				PlayMode.Taiko => stats.RankedScoreTaiko,
				PlayMode.CatchTheBeat => stats.RankedScoreCtb,
				PlayMode.OsuMania => stats.RankedScoreMania,
				_ => 0
			};

			accuracy = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.AccuracyOsu,
				PlayMode.Taiko => stats.AccuracyTaiko,
				PlayMode.CatchTheBeat => stats.AccuracyCtb,
				PlayMode.OsuMania => stats.AccuracyMania,
				_ => 0
			};

			playCount = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.PlaycountOsu,
				PlayMode.Taiko => stats.PlaycountTaiko,
				PlayMode.CatchTheBeat => stats.PlaycountCtb,
				PlayMode.OsuMania => stats.PlaycountMania,
				_ => 0
			};

			RankedScore = rankedScore;
			Accuracy = accuracy;
			PlayCount = playCount;
			TotalScore = totalScore;
			Performance = (short)performance;
		}

		public void AddScore(UserStats stats, long score, bool ranked, PlayMode mode)
		{
			switch (mode)
			{
				case PlayMode.Osu:
					if (ranked)
						stats.RankedScoreOsu += score;
					else
						stats.TotalScoreOsu += score;

					break;

				case PlayMode.Taiko:
					if (ranked)
						stats.RankedScoreTaiko += score;
					else
						stats.TotalScoreTaiko += score;

					break;

				case PlayMode.CatchTheBeat:
					if (ranked)
						stats.RankedScoreCtb += score;
					else
						stats.TotalScoreCtb += score;

					break;

				case PlayMode.OsuMania:
					if (ranked)
						stats.RankedScoreMania += score;
					else
						stats.TotalScoreMania += score;

					break;
			}
		}

        public void AddPlaycount(UserStats stats, PlayMode mode)
        {
            switch (mode)
            {
                case PlayMode.Osu:
                    stats.PlaycountOsu++;
                    break;

                case PlayMode.Taiko:
                    stats.PlaycountTaiko++;
                    break;

                case PlayMode.CatchTheBeat:
                    stats.PlaycountCtb++;
                    break;

                case PlayMode.OsuMania:
                    stats.PlaycountMania++;
                    break;
            }
        }


        // taken from Sora https://github.com/Chimu-moe/Sora/blob/7bba59c8000b440f7f81d2a487a5109590e37068/src/Sora/Database/Models/DBLeaderboard.cs#L200
        public async Task<double> UpdateAccuracy(OyasumiDbContext context, UserStats stats, PlayMode mode)
		{
			var totalAcc = 0d;
			var divideTotal = 0d;
			var i = 0;

			var scores = (await context
				.Scores.AsNoTracking()
				.ToListAsync())
				.Where(s => s.PlayMode == mode)
				.Where(s => s.UserId == Id)
				.Take(500)
				.OrderByDescending(s => s.PerformancePoints);

			foreach (var s in scores)
			{
				var divide = Math.Pow(.95d, i);

				totalAcc += s.Accuracy * divide;
				divideTotal += divide;

				i++;
			}

			var acc = divideTotal > 0 ? totalAcc / divideTotal : 0;

			Accuracy = (float)acc; // Keep accuracy up to date;

			switch (mode)
            {
				case PlayMode.Osu:
					stats.AccuracyOsu = Accuracy;
					break;
				case PlayMode.CatchTheBeat:
					stats.AccuracyCtb = Accuracy;
					break;
				case PlayMode.Taiko:
					stats.AccuracyTaiko = Accuracy;
					break;
				case PlayMode.OsuMania:
					stats.AccuracyMania = Accuracy;
					break;
			}


			return acc;
		}

		// TODO:
		public async Task<double> UpdatePerformance(OyasumiDbContext context, PlayMode mode)
		{
			return 0;
		}

		public async Task Apply(OyasumiDbContext context)
        {
			await context.SaveChangesAsync();
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
