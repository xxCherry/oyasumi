using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Utilities
{
    public class NetUtils
    {
        public static ContentResult Content(string message, int code = 200)
        {
            var result = new ContentResult
            {
                StatusCode = code,
                Content = message
            };

            return result;
        }

        public static async Task<(string countryCode, float latitude, float longitude)> FetchGeoLocation(string ip)
        {
            using var httpClient = new HttpClient();
            var data = (dynamic)JsonConvert.DeserializeObject(await httpClient.GetStringAsync("http://ip-api.com/json/" + ip));

            return (data.countryCode, data.lat, data.lon);
        }
    }
}
