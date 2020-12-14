using Newtonsoft.Json;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.API.ResponseObjects
{
    public class ProfileScoreResponse
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("beatmap")] public Beatmap Beatmap { get; set; }
        [JsonProperty("mods")] public Mods Mods { get; set; }
        [JsonProperty("accuracy")] public double Accuracy { get; set; }
        [JsonProperty("timestamp")] public int Timestamp { get; set; }
        [JsonProperty("pp")] public double Performance { get; set; }
        [JsonProperty("rank")] public string Rank { get; set; }
    }
}