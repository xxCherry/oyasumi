using Newtonsoft.Json;

namespace oyasumi.API.Response
{
    public class ServerStatsResponse
    {
        [JsonProperty("registered")] public int RegisteredUsers { get; set; }
        [JsonProperty("playing")] public int CurrentlyPlaying { get; set; }
    }
}