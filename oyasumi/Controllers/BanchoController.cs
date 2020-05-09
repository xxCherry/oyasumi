using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HOPEless.Bancho;
using oyasumi.Database;
using oyasumi.Objects;
using oyasumi.Events;
using osu.Shared.Serialization;
using HOPEless.Bancho.Objects;
using System.IO;

namespace oyasumi.Controllers
{

    [Route("/")]
    public class BanchoController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            /*var user = new Users
            {
                Username = "kireu",
                UsernameSafe = "kireu",
                Country = "RU",
                Password = UserHelper.GenerateHash("5f4dcc3b5aa765d61d8327deb882cf99")
            };
            var user1 = new UserStats { };  

            Global.DBContext.DBUsers.Add(user);
            Global.DBContext.DBUserStats.Add(user1); 

            await Global.DBContext.SaveChangesAsync(); */
            Global.DBContext = new OyasumiDbContext();
            var body = new MemoryStream();
            await Request.Body.CopyToAsync(body);
            body.Position = 0;

            var ms = new MemoryStream();
            SerializationWriter sw = new SerializationWriter(ms);

            Response.Headers["cho-protocol"] = "19";
            Response.Headers["Keep-Alive"] = "timeout=60, max=100";
            Response.Headers["Content-Type"] = "text/html; charset=UTF-8";
            Response.Headers["Connection"] = "keep-alive";

            Response.StatusCode = 200;

            if (string.IsNullOrEmpty(Request.Headers["osu-token"]))
            {
                var player = await Login.Handle(body, sw);
                Response.Headers["cho-token"] = player.Token;
                body.Position = 0;
            }
            else
            {
                var player = Players.GetPlayerByToken(Request.Headers["osu-token"]);

                if (!(player is null))
                {
                    var packets = BanchoSerializer.DeserializePackets(body).ToList();

                    foreach (var packet in packets)
                    {
                        BanchoEventHandler.Handle(packet, player);
                    }
                    player.WritePackets(sw);
                }
                else
                {
                    Console.WriteLine("Invalid token.");
                    new BanchoPacket(PacketType.ServerRestart, new BanchoInt(0)).WriteToStream(sw);
                }
            }
            ms.Position = 0;
            return File(ms, "application/octet-stream");

        }
    }
}

