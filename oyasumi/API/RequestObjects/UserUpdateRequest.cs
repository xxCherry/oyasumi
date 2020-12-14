using Newtonsoft.Json;

namespace oyasumi.API.RequestObjects
{
    public class UserUpdateRequest
    {
        [JsonProperty("nc_instead_dt")] public bool PreferNightcore { get; set; }
    }
}