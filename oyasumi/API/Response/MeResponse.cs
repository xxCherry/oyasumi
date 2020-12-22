using Newtonsoft.Json;
using oyasumi.Enums;

namespace oyasumi.API.Response
{
    public class MeResponse
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("privileges")] public Privileges Privileges { get; set; }
        [JsonProperty("banned")] public bool Banned { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("nc_instead_dt")] public bool PreferNightcore { get; set; }
    }
}