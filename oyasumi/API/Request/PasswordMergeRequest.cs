using Newtonsoft.Json;

namespace oyasumi.API.Request
{
    public class PasswordMergeRequest
    {
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
    }
}