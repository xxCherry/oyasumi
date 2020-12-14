using Newtonsoft.Json;

namespace oyasumi.API.ResponseObjects
{
    public class LeaderboardResponse
    {
        [JsonProperty("id")] public int Id { get; set; }   
        [JsonProperty("username")] public string Username { get; set; }        
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("level")] public int Level { get; set; }
        [JsonProperty("playcount")] public int Playcount { get; set; }
        [JsonProperty("performance")] public int Performance { get; set; }
        [JsonProperty("accuracy")] public float Accuracy { get; set; }
        [JsonProperty("ss_ranks")] public int SSRanks { get; set; }
        [JsonProperty("s_ranks")] public int SRanks { get; set; }
        [JsonProperty("a_ranks")] public int ARanks { get; set; }

    }
}