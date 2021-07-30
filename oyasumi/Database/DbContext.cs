using System.Collections.Concurrent;
using System.Collections.Generic;
using oyasumi.Database.Models;
using oyasumi.Utilities;

namespace oyasumi.Database
{
    public class DbContext
    {
        public static ConcurrentDictionary<int, VanillaStats> VanillaStats = new();
        public static ConcurrentDictionary<int, RelaxStats> RelaxStats = new();
        public static MultiKeyDictionary<int, string, User> Users = new();
        public static MultiKeyDictionary<int, string, DbBeatmap> Beatmaps = new();
        public static List<DbChannel> Channels = new();
        public static List<DbScore> Scores = new();
        public static List<Friend> Friends = new();

        public static void Load()
        {
            DbReader.Load(@"./oyasumi_database.ch");
        }
        public static void Save()
        {
            DbWriter.Save(@"./oyasumi_database.ch");
        }
    }
}
