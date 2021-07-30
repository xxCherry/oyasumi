using Microsoft.AspNetCore.Http;
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
    public static class NetUtils
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

        public static ContentResult StatusCode(object message, int code)
        {
            var dict = new Dictionary<string, object>
            {
                ["result"] = message
            };
            
            var result = new ContentResult
            {
                ContentType = "application/json", 
                StatusCode = code, 
                Content = JsonConvert.SerializeObject(dict)
            };
            
            return result;
        }
        
        public static ContentResult Error(object message)
        {
            var dict = new Dictionary<string, object>
            {
                ["result"] = message
            };
            
            var result = new ContentResult
            {
                ContentType = "application/json", 
                StatusCode = 401, 
                Content = JsonConvert.SerializeObject(dict)
            };
            
            return result;
        }
        
        public static async Task<(string CountryCode, float Latitude, float Longitude)> FetchGeoLocation(string ip)
        {
            using var httpClient = new HttpClient();
            var data = (dynamic)JsonConvert.DeserializeObject(await httpClient.GetStringAsync("http://ip-api.com/json/" + ip));

            return (data.countryCode, data.lat, data.lon);
        }
    }
}
