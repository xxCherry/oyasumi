using Newtonsoft.Json;

namespace oyasumi.API.Request
{
    public class UserpageUpdateRequest
    {
        [JsonProperty("content")] public string Content { get; set; }
    }
}