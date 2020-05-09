using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oyasumi.Objects
{
    public class Players
    {
        public static List<Player> PlayerList = new List<Player>();
        public static Player GetPlayerByToken(string token)
        {
            return PlayerList.FirstOrDefault(x => x.Token == token);
        }
        public static Player GetPlayerById(int Id)
        {
            return PlayerList.FirstOrDefault(x => x.Id == Id);
        }
    }
}
