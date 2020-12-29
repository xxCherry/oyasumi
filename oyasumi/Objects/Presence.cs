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
using oyasumi.Interfaces;
using oyasumi.Chat.Objects;
using System.Threading;
using oyasumi.Managers;

namespace oyasumi.Objects
{
	public class Presence
	{

		public readonly string Token;
		public readonly string Username;

		public readonly User User;

		public Match CurrentMatch;

		public Presence Spectating;
		public Channel SpectatorChannel;
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

		public class ActionStatus
		{
			public ActionStatuses Status { get; set; }
			public string StatusText { get; set; }
			public string BeatmapChecksum { get; set; }
			public Mods CurrentMods { get; set; }
			public PlayMode CurrentPlayMode { get; set; }
			public int BeatmapId { get; set; }
		}

		public ActionStatus Status;
		public long RankedScore;
        public double Accuracy;
		public int PlayCount;
		public long TotalScore;
		public short Performance;
		// --

		public int LastPing;
		public Score LastScore;
		public Beatmap LastNp;

		public BanchoPermissions BanchoPermissions;
		
        private readonly ConcurrentQueue<Packet> _packetQueue = new ();
		public readonly ConcurrentQueue<ScheduledCommand> CommandQueue = new ();
		public readonly ConcurrentDictionary<string, ScheduledCommand> ProcessedCommands = new();
		

		private static readonly List<string> _countryCodes = new List<string>
		{
					"XX","AP","EU","AD","AE","AF","AG","AI","AL","AM","AN","AO","AQ","AR",
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
			Privileges = user.Privileges;
			Status = new()
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
			Status = new ActionStatus
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

		public async Task GetOrUpdateUserStats(OyasumiDbContext context, LeaderboardMode lbMode, bool update)
		{
			var exists = Base.UserStatsCache[lbMode].TryGetValue(User.Id, out var cachedStats);

			IStats stats = null;

			if (!update && exists) 
				stats = cachedStats; 
			else
			{
				stats = lbMode switch
				{
					LeaderboardMode.Vanilla => await context.VanillaStats.AsAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == User.Id),
					LeaderboardMode.Relax => await context.RelaxStats.AsAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == User.Id)
				};

				if (!exists)
					Base.UserStatsCache[lbMode].TryAdd(User.Id, stats);

				Base.UserStatsCache[lbMode][User.Id] = stats;
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

		public void AddScore(IStats stats, long score, bool ranked, PlayMode mode)
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

        public void AddPlaycount(IStats stats, PlayMode mode)
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
        public async Task<double> UpdateAccuracy(OyasumiDbContext context, IStats stats, PlayMode mode, LeaderboardMode lbMode)
		{
			var totalAcc = 0d;
			var divideTotal = 0d;
			var i = 0;

			var scores = (await context
				.Scores.AsNoTracking()
				.ToListAsync())
				.Where(s => s.PlayMode == mode)
				.Where(s => s.UserId == Id)
				.Where(s => s.Relaxing == (lbMode == LeaderboardMode.Relax))
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
					stats.AccuracyOsu = (float)Accuracy;
					break;
				case PlayMode.CatchTheBeat:
					stats.AccuracyCtb = (float)Accuracy;
					break;
				case PlayMode.Taiko:
					stats.AccuracyTaiko = (float)Accuracy;
					break;
				case PlayMode.OsuMania:
					stats.AccuracyMania = (float)Accuracy;
					break;
			}


			return acc;
		}

		public async Task<int> UpdateRank(OyasumiDbContext context, IStats stats, PlayMode mode, LeaderboardMode lbMode)
        {
			var oldRank = mode switch
			{
				PlayMode.Osu => stats.RankOsu,
				PlayMode.Taiko => stats.RankTaiko,
				PlayMode.CatchTheBeat => stats.RankCtb,
				PlayMode.OsuMania => stats.RankMania,
				_ => 0
			};

			IAsyncEnumerable<IStats> usersStats = lbMode switch
			{
				LeaderboardMode.Vanilla => context.VanillaStats.AsAsyncEnumerable(),
				LeaderboardMode.Relax => context.RelaxStats.AsAsyncEnumerable()
			};

			var newRank = await CalculateRank(context, stats, usersStats, mode, lbMode);

			if (newRank != oldRank)
			{
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
						
						if (Base.UserStatsCache[lbMode].TryGetValue(snipedStats.Id, out var cachedStats))
							Base.UserStatsCache[lbMode].TryUpdate(snipedStats.Id, snipedStats, cachedStats);

						var pr = PresenceManager.GetPresenceById(snipedStats.Id);
						
						if (pr is not null)
							pr.Rank = newRank + i + 1;
					}
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

		public async Task<int> CalculateRank(OyasumiDbContext context, IStats stats, IAsyncEnumerable<IStats> usersStats, PlayMode mode, LeaderboardMode lbMode)
        {
			var newRank = mode switch 
			{
				PlayMode.Osu => stats.PerformanceOsu > 0 ? await usersStats.CountAsync(x => x.PerformanceOsu > stats.PerformanceOsu) : -1,
				PlayMode.Taiko => stats.PerformanceTaiko > 0 ? await usersStats.CountAsync(x => x.PerformanceTaiko > stats.PerformanceTaiko) : -1,
				PlayMode.CatchTheBeat => stats.PerformanceCtb > 0 ? await usersStats.CountAsync(x => x.PerformanceCtb > stats.PerformanceCtb) : -1,
				PlayMode.OsuMania => stats.PerformanceMania > 0 ? await usersStats.CountAsync(x => x.PerformanceMania > stats.PerformanceMania) : -1,
				_ => -1
			} + 1;

			return newRank;
        }

		public async Task<double> UpdatePerformance(OyasumiDbContext context, IStats stats, PlayMode mode, LeaderboardMode lbMode)
		{
			var scores = await context
						.Scores
						.AsAsyncEnumerable()
						.Where(x => x.PlayMode == mode && x.Completed == CompletedStatus.Best && x.UserId == Id && x.Relaxing == (lbMode == LeaderboardMode.Relax))
						.OrderByDescending(x => x.PerformancePoints)
						.Take(500)
						.ToListAsync();

			var totalPerformance = scores
				.Select((t, i) => 
					Math.Round(Math.Round(t.PerformancePoints) * Math.Pow(0.95, i)))
				.Sum();

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

		public bool WaitForCommandArguments(string cmd, out string[] args)
        {
			ScheduledCommand schCommand = null;

			args = Array.Empty<string>();

			if (!ProcessedCommands.TryGetValue(cmd, out schCommand)) 
				return true;

			ProcessedCommands.Remove(cmd, out _);

			if (!schCommand.NoErrors)
				args = null;
			else 
				args = schCommand.Args;

			return false;
		}

		public async Task Apply(OyasumiDbContext context) => await context.SaveChangesAsync();

		public void PacketEnqueue(Packet p) => _packetQueue.Enqueue(p);
		public void CommandEnqueue(ScheduledCommand c) => CommandQueue.Enqueue(c);
		
		public async Task<byte[]> WritePackets()
		{
			var writer = new PacketWriter();
			while (_packetQueue.TryDequeue(out var p))
				await writer.Write(p);

			return writer.ToBytes();
		}
	}
}
