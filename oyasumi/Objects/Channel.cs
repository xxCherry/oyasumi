using oyasumi.Managers;
using oyasumi.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public class Channel
    {
        public ConcurrentDictionary<int, Presence> Presences = new ConcurrentDictionary<int, Presence>();

        public string Name => RawName.StartsWith("multi_") ? "#multiplayer" : RawName.StartsWith("spect_") ? "#spectator" : RawName;
        public string RawName;
        public string Description;
        public int UserCount;
        public bool PublicWrite;

        public Channel(string name, string description, int userCount, bool publicWrite)
        {
            RawName = name;
            Description = description;
            UserCount = userCount;
            PublicWrite = publicWrite;
        }
    }
}
