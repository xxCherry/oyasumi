﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Devcorner.NIdenticon;

namespace oyasumi.Controllers
{
    [Route("/{id:int}")]
    public class AvatarController : OyasumiController
    {
        public async Task<IActionResult> Index(int id)
        {
            if (System.IO.File.Exists($"./data/avatars/{id}.png"))
            {
                var file = await System.IO.File.ReadAllBytesAsync($"./data/avatars/{id}.png");
                return Bytes(file);
            }

            var user = Base.UserCache[id];

            if (user is null)
                return StatusCode(404);

            var generator = new IdenticonGenerator();
            await using var ms = new MemoryStream();

            generator.Create(user.Username, new Size(64, 64)).Save(ms, ImageFormat.Png);
            
            await System.IO.File.WriteAllBytesAsync($"./data/avatars/{id}.png", ms.ToArray());

            return Bytes(ms.ToArray());
        }
    }
}
