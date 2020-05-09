using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using HOPEless.osu;
using osu.Shared;
using osu.Shared.Serialization;
using oyasumi.Database;

namespace oyasumi.Objects
{
    public class Player
    {
        public int Id;
        public string Username;
        public string Token;

        public int Timezone = 0;
        public byte Country = 0;

        public long loginTime;

        public BanchoUserStatus Status = new BanchoUserStatus();

        public short Performance; // using long since performance can be both as score and as pp
        public PlayerRank Permissions = PlayerRank.Supporter; // Free direct

        private static ConcurrentQueue<BanchoPacket> PacketsQueue = new ConcurrentQueue<BanchoPacket>();
        private static SerializationWriter sw = new SerializationWriter(new MemoryStream());

        public Player(int userId, int timeZone)
        {
            Id = userId;
            Username = Global.Factory.Get().DBUsers.Where(x => x.Id == Id).Select(x => x.Username).FirstOrDefault();
            Timezone = timeZone;
            Performance = GetPerformance();
            Token = Guid.NewGuid().ToString();
            loginTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            Players.PlayerList.Add(this);
        }

        public void PacketEnqueue(BanchoPacket packet)
        {
            PacketsQueue.Enqueue(packet);
        }

        public void Disconnect()
        {
            Players.PlayerList.Remove(this);
        }

        public void WritePackets(SerializationWriter sw)
        {
            foreach (var p in PacketsQueue)
            {
                p.WriteToStream(sw);
            }
        }

        public short GetPerformance()
        {
            // TODO: Make it more cleaner if possible
            var performance = Status.PlayMode switch
            {
                GameMode.Standard => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PerformanceOsu).FirstOrDefault(),
                GameMode.Taiko => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PerformanceTaiko).FirstOrDefault(),
                GameMode.CatchTheBeat => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PerformanceCtb).FirstOrDefault(),
                GameMode.Mania => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PerformanceMania).FirstOrDefault(),
                _ => 0
            };

            if (performance > short.MaxValue)
                return 0; // force client to show score instead of pp

            return (short)performance;
        }

        public long GetTotalScore()
        {
            // TODO: Make it more cleaner if possible
            return Status.PlayMode switch
            {
                GameMode.Standard => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.TotalScoreOsu).FirstOrDefault(),
                GameMode.Taiko => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.TotalScoreTaiko).FirstOrDefault(),
                GameMode.CatchTheBeat => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.TotalScoreCtb).FirstOrDefault(),
                GameMode.Mania => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.TotalScoreMania).FirstOrDefault(),
                _ => 0
            };
        }

        public float GetAccuracy()
        {
            // TODO: Make it more cleaner if possible
            return Status.PlayMode switch
            {
                GameMode.Standard => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.AccuracyOsu).FirstOrDefault(),
                GameMode.Taiko => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.AccuracyTaiko).FirstOrDefault(),
                GameMode.CatchTheBeat => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.AccuracyCtb).FirstOrDefault(),
                GameMode.Mania => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.AccuracyMania).FirstOrDefault(),
                _ => 0
            };
        }

        public long GetRankedScore()
        {
            // TODO: Make it more cleaner if possible
            return Status.PlayMode switch
            {
                GameMode.Standard => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.RankedScoreOsu).FirstOrDefault(),
                GameMode.Taiko => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.RankedScoreTaiko).FirstOrDefault(),
                GameMode.CatchTheBeat => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.RankedScoreCtb).FirstOrDefault(),
                GameMode.Mania => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.RankedScoreMania).FirstOrDefault(),
                _ => 0
            };
        }

        public int GetPlaycount()
        {
            // TODO: Make it more cleaner if possible
            return Status.PlayMode switch
            {
                GameMode.Standard => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PlaycountOsu).FirstOrDefault(),
                GameMode.Taiko => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PlaycountTaiko).FirstOrDefault(),
                GameMode.CatchTheBeat => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PlaycountCtb).FirstOrDefault(),
                GameMode.Mania => Global.Factory.Get().DBUserStats.Where(x => x.Id == Id).Select(x => x.PlaycountMania).FirstOrDefault(),
                _ => 0
            };
        }

        public BanchoUserData ToUserData()
        {
            return new BanchoUserData()
            {
                UserId = Id,
                Status = Status,
                Accuracy = GetAccuracy(),
                Performance = GetPerformance(),
                Playcount = GetPlaycount(),
                Rank = 1, // TODO: Implement ranks by cache
                RankedScore = GetRankedScore(),
                TotalScore = GetTotalScore()
            };
        }
        public BanchoUserPresence ToUserPresence()
        {
            return new BanchoUserPresence()
            {
                UserId = Id,
                Username = Username,
                CountryCode = Country,
                Latitude = 1.0f,
                Longitude = 5.0f,
                Permissions = Permissions,
                PlayMode = Status.PlayMode,
                Rank = 1, // TODO: Implement ranks by cache
                Timezone = Timezone,
                UsesOsuClient = true
            };
        }
    }
}
