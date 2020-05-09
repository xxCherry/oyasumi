using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public class ConfigScheme
    {
        public string Database;
        public string Username;
        public string Password;
    }
    public class Config
    {
        public static ConfigScheme Get()
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
            return JsonConvert.DeserializeObject<ConfigScheme>(File.ReadAllText("config.json"));
        }
    }
}