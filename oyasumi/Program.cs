using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HOPEless.Bancho;
using oyasumi.Database;
using oyasumi.Objects;
using oyasumi.Events;

namespace oyasumi
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Global.DBContext = new OyasumiDbContext();
            Console.WriteLine("Custom server for osu!\n  by Cherry, 2020");
            var listener = new HttpListener();
            listener.Prefixes.Add("http://+:1337/");
            listener.Start(); 

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

            while (true)
            {
                var context = listener.GetContext();
                Global.Request = context.Request;
                Global.Response = context.Response;
                var body = new MemoryStream();
                await Global.Request.InputStream.CopyToAsync(body);
                body.Position = 0;

                Global.Response.AddHeader("cho-protocol", "19");
                Global.Response.AddHeader("Keep-Alive", "timeout=5, max=100");
                Global.Response.AddHeader("Content-Type", "text/html; charset=UTF-8");
                Global.Response.KeepAlive = true;
                if (string.IsNullOrEmpty(Global.Request.Headers["osu-token"]))
                {
                    await Login.Handle(body);
                    body.Position = 0;
                }
                else
                {
                    var player = Players.GetPlayerByToken(Global.Request.Headers["osu-token"]);
                    if (!(player is null))
                    {
                        var packets = BanchoSerializer.DeserializePackets(body).ToList();

                        foreach (var packet in packets)
                        {
                            BanchoEventHandler.Handle(packet, player);
                        }
                        await player.SendPackets();
                    } 
                }
            }
        }
    }
}
