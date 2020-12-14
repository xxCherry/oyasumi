using Newtonsoft.Json;

namespace oyasumi.API.ResponseObjects
{
    public class ProfileResponse
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("place")] public int Place { get; set; }
        [JsonProperty("level")] public int Level { get; set; }
        [JsonProperty("playcount")] public int Playcount { get; set; }
        [JsonProperty("performance")] public int Performance { get; set; }
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("ss_ranks")] public int SSRanks { get; set; }
        [JsonProperty("s_ranks")] public int SRanks { get; set; }
        [JsonProperty("a_ranks")] public int ARanks { get; set; }
        [JsonProperty("userpage_context")] public string UserpageContent { get; set; }
        [JsonProperty("account_created_at")] public int AccountCreatedAt { get; set; }
        [JsonProperty("replays_watched")] public int ReplaysWatched { get; set; }
        [JsonProperty("ranked_score")] public long RankedScore { get; set; }
        [JsonProperty("total_score")] public long TotalScore { get; set; }
        [JsonProperty("total_hits")] public int TotalHits { get; set; }
        [JsonProperty("accuracy")] public float Accuracy { get; set; }

        //public int verification_type { get; set; }

        //public int is_supporter { get; set; }
    }
}