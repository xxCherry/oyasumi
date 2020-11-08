﻿using System;
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
		public List<Presence> Spectators = new ();

		public ConcurrentDictionary<string, Channel> Channels = new ();

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

		private static readonly List<string> _countryCodes = new List<string>
		{
					"--","AP","EU","AD","AE","AF","AG","AI","AL","AM","AN","AO","AQ","AR",
					"AS","AT","AU","AW","AZ","BA","BB","BD","BE","BF","BG","BH","BI","BJ",
					"BM","BN","BO","BR","BS","BT","BV","BW","BY","BZ","CA","CC","CD","CF",
					"CG","CH","CI","CK","CL","CM","CN","CO","CR","CU","CV","CX","CY","CZ",
					"DE","DJ","DK","DM","DO","DZ","EC","EE","EG","EH","ER","ES","ET","FI",
					"FJ","FK","FM","FO","FR","FX","GA","GB","GD","GE","GF","GH","GI","GL",
					"GM","GN","GP","GQ","GR","GS","GT","GU","GW","GY","HK","HM","HN","HR",
					"HT","HU","ID","IE","IL","IN","IO","IQ","IR","IS","IT","JM","JO","JP",
					"KE","KG","KH","KI","KM","KN","KP","KR","KW","KY","KZ","LA","LB","LC",
					"LI","LK","LR","LS","LT","LU","LV","LY","MA","MC","MD","MG","MH","MK",
					"ML","MM","MN","MO","MP","MQ","MR","MS","MT","MU","MV","MW","MX","MY",
					"MZ","NA","NC","NE","NF","NG","NI","NL","NO","NP","NR","NU","NZ","OM",
					"PA","PE","PF","PG","PH","PK","PL","PM","PN","PR","PS","PT","PW","PY",
					"QA","RE","RO","RU","RW","SA","SB","SC","SD","SE","SG","SH","SI","SJ",
					"SK","SL","SM","SN","SO","SR","ST","SV","SY","SZ","TC","TD","TF","TG",
					"TH","TJ","TK","TM","TN","TO","TL","TR","TT","TV","TW","TZ","UA","UG",
					"UM","US","UY","UZ","VA","VC","VE","VG","VI","VN","VU","WF","WS","YE",
					"YT","RS","ZA","ZM","ME","ZW","A1","A2","O1","AX","GG","IM","JE","BL",
					"MF"
		};

		public Presence(User user, int timezone)
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

			Timezone = (byte)(timezone + 24);

			CountryCode = (byte)_countryCodes.IndexOf(user.Country);

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

			var performance = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.PerformanceOsu,
				PlayMode.Taiko => stats.PerformanceTaiko,
				PlayMode.CatchTheBeat => stats.PerformanceCtb,
				PlayMode.OsuMania => stats.PerformanceMania,
				_ => 0
			};

			if (performance > short.MaxValue)
				performance = 0;

			var totalScore = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.TotalScoreOsu,
				PlayMode.Taiko => stats.TotalScoreTaiko,
				PlayMode.CatchTheBeat => stats.TotalScoreCtb,
				PlayMode.OsuMania => stats.TotalScoreMania,
				_ => 0
			};

			var rankedScore = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.RankedScoreOsu,
				PlayMode.Taiko => stats.RankedScoreTaiko,
				PlayMode.CatchTheBeat => stats.RankedScoreCtb,
				PlayMode.OsuMania => stats.RankedScoreMania,
				_ => 0
			};

			var accuracy = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.AccuracyOsu,
				PlayMode.Taiko => stats.AccuracyTaiko,
				PlayMode.CatchTheBeat => stats.AccuracyCtb,
				PlayMode.OsuMania => stats.AccuracyMania,
				_ => 0
			};

			var playCount = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.PlaycountOsu,
				PlayMode.Taiko => stats.PlaycountTaiko,
				PlayMode.CatchTheBeat => stats.PlaycountCtb,
				PlayMode.OsuMania => stats.PlaycountMania,
				_ => 0
			};

			var rank = Status.CurrentPlayMode switch
			{
				PlayMode.Osu => stats.RankOsu,
				PlayMode.Taiko => stats.RankTaiko,
				PlayMode.CatchTheBeat => stats.RankCtb,
				PlayMode.OsuMania => stats.RankMania,
				_ => 0
			};

			RankedScore = rankedScore;
			Accuracy = accuracy;
			PlayCount = playCount;
			TotalScore = totalScore;
			Performance = (short)performance;
			Rank = rank;
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

		public async Task<int> UpdateRank(OyasumiDbContext context, UserStats stats, PlayMode mode)
        {
			var oldRank = mode switch
			{
				PlayMode.Osu => stats.RankOsu,
				PlayMode.Taiko => stats.RankTaiko,
				PlayMode.CatchTheBeat => stats.RankCtb,
				PlayMode.OsuMania => stats.RankMania,
				_ => 0
			};

			var newRank = await CalculateRank(context, stats, mode);

			if (newRank != oldRank)
			{
				var usersStats = context.UsersStats.AsAsyncEnumerable();

				// r = newRank - oldRank (range of users that we sniped)
				/* for (var i = 0; i < r; i++)
				 *		currentRank = snipedStats.Rank;
				 *		
				 *		if (currentRank == newRank + i)
				 *			snipedRank = newRank + (i + 1);
				 */


				// value is always positive i guess
				var range = oldRank - newRank;

				for (var i = 0; i < range; i++)
                {
					var snipedStats = await usersStats.FirstOrDefaultAsync(x => mode switch
                    {
                        PlayMode.Osu => x.RankOsu,
                        PlayMode.Taiko => x.RankTaiko,
                        PlayMode.CatchTheBeat => x.RankCtb,
                        PlayMode.OsuMania => x.RankMania,
                        _ => throw new NotImplementedException()
                    } == newRank + i);

					if (snipedStats is not null)
					{
						switch (mode)
						{
							case PlayMode.Osu:
								snipedStats.RankOsu = newRank + i + 1;
								break;
							case PlayMode.CatchTheBeat:
								snipedStats.RankCtb = newRank + i + 1;
								break;
							case PlayMode.Taiko:
								snipedStats.RankTaiko = newRank + i + 1;
								break;
							case PlayMode.OsuMania:
								snipedStats.RankMania = newRank + i + 1;
								break;
						}
					}

					if (Base.UserStatsCache.TryGetValue(snipedStats.Id, out var cachedStats))
						Base.UserStatsCache.TryUpdate(snipedStats.Id, snipedStats, cachedStats);
				}


				switch (mode)
				{
					case PlayMode.Osu:
						stats.RankOsu = newRank;
						break;
					case PlayMode.CatchTheBeat:
						stats.RankCtb = newRank;
						break;
					case PlayMode.Taiko:
						stats.RankTaiko = newRank;
						break;
					case PlayMode.OsuMania:
						stats.RankMania = newRank;
						break;
				}
				await context.SaveChangesAsync();
			}
			return newRank;
		}

		public async Task<int> CalculateRank(OyasumiDbContext context, UserStats stats, PlayMode mode)
        {
			var userStats = context.UsersStats.AsAsyncEnumerable();

			var newRank = mode switch 
			{
				PlayMode.Osu => stats.PerformanceOsu > 0 ? await userStats.CountAsync(x => x.PerformanceOsu > stats.PerformanceOsu) : -1,
				PlayMode.Taiko => stats.PerformanceTaiko > 0 ? await userStats.CountAsync(x => x.PerformanceTaiko > stats.PerformanceTaiko) : -1,
				PlayMode.CatchTheBeat => stats.PerformanceCtb > 0 ? await userStats.CountAsync(x => x.PerformanceCtb > stats.PerformanceCtb) : -1,
				PlayMode.OsuMania => stats.PerformanceMania > 0 ? await userStats.CountAsync(x => x.PerformanceMania > stats.PerformanceMania) : -1,
				_ => -1
			} + 1;

			return newRank;
        }

		public async Task<double> UpdatePerformance(OyasumiDbContext context, UserStats stats, PlayMode mode)
		{
			var scores = await context
						.Scores
						.AsAsyncEnumerable()
						.Where(x => x.PlayMode == mode && x.Completed == CompletedStatus.Best && x.UserId == Id)
						.OrderByDescending(x => x.PerformancePoints)
						.Take(500)
						.ToListAsync();

			var totalPerformance = 0d;

			for (var i = 0; i < scores.Count; i++)
				totalPerformance += Math.Round(Math.Round(scores[i].PerformancePoints) * Math.Pow(0.95, i));

			if (totalPerformance > short.MaxValue)
				Performance = 0;
			else
				Performance = (short)totalPerformance;

			switch (mode)
			{
				case PlayMode.Osu:
					stats.PerformanceOsu = (int)totalPerformance;
					break;
				case PlayMode.CatchTheBeat:
					stats.PerformanceCtb = (int)totalPerformance;
					break;
				case PlayMode.Taiko:
					stats.PerformanceTaiko = (int)totalPerformance;
					break;
				case PlayMode.OsuMania:
					stats.PerformanceMania = (int)totalPerformance;
					break;
			}

			return totalPerformance;
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
