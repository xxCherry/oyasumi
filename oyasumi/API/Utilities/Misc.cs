using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using oyasumi.Objects;

namespace oyasumi.API.Utilities
{
    public class CaptchaResponse
    {
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("timestamp")] public string Timestamp { get; set; }
        [JsonProperty("host")] public string Host { get; set; }
    }
    
    public class Misc
    {
        public static bool VerifyToken(string token)
        {
            return true;
            return token is not null && Base.TokenCache[token] is not null;
        }

        public static async Task<bool> VerifyCaptcha(string key, string userIp)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", Config.Properties.RecaptchaPrivate),
                new KeyValuePair<string, string>("response", key),
                new KeyValuePair<string, string>("remoteip", userIp)
            });
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

            var responseObject = JsonConvert.DeserializeObject<CaptchaResponse>(await response.Content.ReadAsStringAsync());
            return responseObject.Success;
        }
    }
}