using Newtonsoft.Json;

namespace oyasumi.API.RequestObjects
{
    public class UserpageUpdateRequest
    {
        [JsonProperty("content")] public string Content { get; set; }
    }
}