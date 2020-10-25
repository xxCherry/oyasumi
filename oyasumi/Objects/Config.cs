using System.IO;
using Newtonsoft.Json;

namespace oyasumi.Objects
{
    public class ConfigScheme
    {
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BeatmapMirror { get; set; }
    }

    public class Config
    {
        public static ConfigScheme Properties
        {
            get
            {
                if (!File.Exists("config.json"))
                {
                    File.WriteAllText("config.json", JsonConvert.SerializeObject(new ConfigScheme
                    {
                        Database = "oyasumi",
                        Username = "root",
                        Password = ""
                    }));
                } 
                return JsonConvert.DeserializeObject<ConfigScheme>(File.ReadAllText(@"config.json"));
            }
        }
    }
}