using Newtonsoft.Json;

namespace oyasumi.API.Request
{
    public class UserUpdateRequest
    {
        [JsonProperty("nc_instead_dt")] public bool PreferNightcore { get; set; }
    }
}