using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.API.Response;
using oyasumi.Database;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.API.Controllers
{
    // Uncatigorized stuff
    [ApiController]
    [Route("/api/")]
    public class GeneralController : Controller
    {
        [HttpGet("/stats/users")]
        public IActionResult Stats()
        {
            var registered = DbContext.Users.Values.Length;
            var online = PresenceManager.Presences.Values.ToList().Count;

            return Content(JsonConvert.SerializeObject(new ServerStatsResponse
            {
                RegisteredUsers = registered,
                CurrentlyPlaying = online - 1
            }));
        }
    }
}