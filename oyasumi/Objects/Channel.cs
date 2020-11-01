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
        public ConcurrentList<Presence> Presences = new ConcurrentList<Presence>();

        public string Name;
        public string Description;
        public int UserCount;

        public Channel(string name, string description, int userCount)
        {
            Name = name;
            Description = description;
            UserCount = userCount;
        }
    }
}
