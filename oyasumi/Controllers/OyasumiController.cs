using Microsoft.AspNetCore.Mvc;

namespace oyasumi.Controllers
{
    public class OyasumiController : Controller
    {
        [NonAction]
        public FileContentResult Bytes(byte[] data)
        {
            return new FileContentResult(data, "application/octet-stream");
        }

        [NonAction]
        public FileContentResult NoTokenBytes(byte[] data)
        {
            Response.Headers["cho-token"] = "no-token";

            return new FileContentResult(data, "application/octet-stream");
        }
    }
}
