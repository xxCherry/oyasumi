using Newtonsoft.Json;

namespace oyasumi.API.RequestObjects
{
    public class RegistrationRequest
    {
        [JsonProperty("login")] public string Login { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("ip")] public string Ip { get; set; }
        [JsonProperty("captcha_key")] public string CaptchaKey { get; set; }
    }
}