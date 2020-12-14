using Newtonsoft.Json;

namespace oyasumi.API.RequestObjects
{
    public class LoginRequest
    {
        [JsonProperty("login")] public string Login { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonProperty("captcha_key")] public string CaptchaKey { get; set; }
    }
}