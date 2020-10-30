using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Controllers
{
	[Route("/")]
	public class BanchoController : Controller
	{
		private readonly OyasumiDbContext _context;

		public BanchoController(OyasumiDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index([FromHeader(Name = "osu-token")] string token)
		{
			if (Request.Method == "GET")
				return Ok("oyasumi - the osu! server.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			if (string.IsNullOrEmpty(token))
			{
				var (username, password) = await Request.Body.ParseLoginDataAsync();

				User dbUser = Base.UserCache[username];

				if (Base.UserCache[username] == default)
				{
					dbUser = _context.Users.AsNoTracking().Where(x => x.UsernameSafe == username.ToSafe()).Take(1).FirstOrDefault();
					Base.UserCache.Add(username, dbUser.Id, dbUser);
				}

				if (dbUser is null)
				{
					Response.Headers["cho-token"] = "no-token";
					return File(await BanchoPacketLayouts.LoginReplyAsync(LoginReplies.WrongCredentials),
						"application/octet-stream");
				}

				if (!Base.PasswordCache.TryGetValue(password, out _))
				{
					if (!Crypto.VerifyPassword(password, dbUser.Password))
					{
						Response.Headers["cho-token"] = "no-token";
						return File(await BanchoPacketLayouts.LoginReplyAsync(LoginReplies.WrongCredentials),
							"application/octet-stream");
					}
					Base.PasswordCache.TryAdd(password, dbUser.Password);
				}

				var presence = new Presence(dbUser);

				PresenceManager.Add(presence);

				await presence.GetOrUpdateUserStats(_context, false);

				presence.ProtocolVersion(19);
				presence.LoginReply(presence.Id);

				presence.Notification("Welcome to oyasumi.");

				presence.UserPresence(); 
				presence.UserStats();
				presence.UserPermissions(BanchoPermissions.Peppy);

				presence.FriendList(new List<int> { presence.Id });

				presence.UserPresenceSingle(presence.Id);

				presence.ChatChannelListingComplete(0);
				presence.ChatChannelJoinSuccess("#osu");
				presence.ChatChannelAvailable("#osu", "Default osu! channel", 1);

				foreach (var pr in PresenceManager.Presences.Values) // go for each presence
				{
					pr.UserPresence(presence); // send us to users
					presence.UserPresence(pr); // send users to us
				}

				var bytes = await presence.WritePackets();

				Response.Headers["cho-token"] = presence.Token;
				Response.Headers["cho-protocol"] = "19";

				presence.Notification("Login took: " + stopwatch.Elapsed.TotalMilliseconds + "ms");

				stopwatch.Stop();

				return File(bytes, "application/octet-stream");
			}
			else
			{
				var presence = PresenceManager.GetPresenceByToken(Request.Headers["osu-token"]);

				if (presence is null)
				{
					Response.Headers["cho-token"] = "no-token";
					return File(await BanchoPacketLayouts.BanchoRestart(0), "application/octet-stream");
				}

				await using var ms = new MemoryStream();
				await Request.Body.CopyToAsync(ms);            
				ms.Position = 0;

				var packets = PacketReader.Parse(ms);

				foreach (var packet in packets)
				{
					if (!Base.PacketImplCache.TryGetValue(packet.Type, out var handle))
					{
						MethodInfo meth = null;
						if (!Base.MethodCache.TryGetValue(packet.Type, out var packetImpl))
						{
							var meths = Base.Types.SelectMany(type => type.GetMethods());

							foreach (var m in meths)
							{
								if (m?.GetCustomAttribute<PacketAttribute>()?.PacketType != packet.Type)
									continue;

								meth = m;

								if (meth is not null)
									Base.MethodCache.TryAdd(packet.Type, meth);

								break;
							}
						}
						else
							meth = packetImpl;

						if (meth is null)
						{
							// not handled
							Console.WriteLine(packet.Type.ToString());
							return Ok();
						}

						var packetParam = Expression.Parameter(typeof(Packet), "p");
						var presenceParam = Expression.Parameter(typeof(Presence), "pr");
						var call = Expression.Call(null, meth, packetParam, presenceParam);
						var lambda = Expression.Lambda<Action<Packet, Presence>>(call, packetParam, presenceParam);
						handle = lambda.CompileFast();

						Base.PacketImplCache.TryAdd(packet.Type, handle);

						handle(packet, presence);
					}
					else
                    {
						handle(packet, presence);
                    }
				}

				var bytes = await presence.WritePackets();

				return File(bytes, "application/octet-stream");
			}
		}
	}
}