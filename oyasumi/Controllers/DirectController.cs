using Microsoft.AspNetCore.Mvc;
using oyasumi.Objects;

namespace oyasumi.Controllers
{
    public class DirectController : OyasumiController
    {
        [HttpGet]
        [Route("/d/{id}")]
        public IActionResult Index(string id) => 
            Redirect($"{Config.Properties.BeatmapMirror}/d/{id}");
    }
}
