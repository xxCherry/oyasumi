using Newtonsoft.Json;

namespace oyasumi.API.RequestObjects
{
    public class UserUpdatePasswordRequest
    {
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("new_password")] public string NewPassword { get; set; }
        [JsonProperty("current_password")] public string CurrentPassword { get; set; }
    }
}