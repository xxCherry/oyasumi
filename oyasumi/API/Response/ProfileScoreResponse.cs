using Newtonsoft.Json;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.API.Response
{
    public class ProfileScoreResponse
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("beatmap")] public Beatmap Beatmap { get; set; }
        [JsonProperty("mods")] public Mods Mods { get; set; }
        [JsonProperty("50_count")] public int Count50 { get; set; }
        [JsonProperty("100_count")] public int Count100 { get; set; }
        [JsonProperty("300_count")] public int Count300 { get; set; }
        [JsonProperty("max_combo")] public int Combo { get; set; }
        [JsonProperty("accuracy")] public double Accuracy { get; set; }
        [JsonProperty("timestamp")] public int Timestamp { get; set; }
        [JsonProperty("pp")] public double Performance { get; set; }
        [JsonProperty("rank")] public string Rank { get; set; }
    }
}