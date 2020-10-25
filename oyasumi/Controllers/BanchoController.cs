using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
		private readonly ILogger<BanchoController> _logger;
			
		public BanchoController(ILogger<BanchoController> logger)
		{
			_logger = logger;
		}

		public async Task<IActionResult> Index([FromHeader(Name = "osu-token")] string token)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			if (Request.Method == "GET")
				return Ok("oyasumi - the osu! server.");
			
			if (string.IsNullOrEmpty(token))
			{
				var (username, password) = await Request.Body.ParseLoginDataAsync();

				User dbUser = null;

				if (Base.UserCache.TryGetValue(username, out var user))
					dbUser = user;
				else
				{
					var dbContext = OyasumiDbContextFactory.Get();
					dbUser = dbContext.Users.AsQueryable().Where(x => x.UsernameSafe == username.ToSafe()).Take(1).FirstOrDefault();
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

				var presence = new Presence(dbUser.Id, username);

				PresenceManager.Add(presence);

				if (user is null)
					Base.UserCache.TryAdd(username, dbUser);

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

				await foreach (var p in packets)
				{
					var meths = Base.Types.SelectMany(type => type.GetMethods());
					MethodInfo meth = null;
					if (!Base.MethodCache.TryGetValue(p.Type, out var packetImpl))
					{
						foreach (var m in meths)
						{
							if (m?.GetCustomAttribute<PacketAttribute>()?.PacketType != p.Type)
								continue;

							meth = m;
								
							if (meth is not null)
								Base.MethodCache.TryAdd(p.Type, meth);
								
							break;
						}
					}
					else
					{
						meth = packetImpl;
					}

					if (meth is null)
					{
						// not handled
						Console.WriteLine(p.Type.ToString());
						return Ok();
					}

					var packetParam = Expression.Parameter(typeof(Packet), "p");
					var presenceParam = Expression.Parameter(typeof(Presence), "pr");
					var call = Expression.Call(null, meth, packetParam, presenceParam);
					var lambda = Expression.Lambda<Action<Packet, Presence>>(call, packetParam, presenceParam);
					var handle = lambda.Compile();
					
					handle(p, presence);
				}

				var bytes = await presence.WritePackets();

				return File(bytes, "application/octet-stream");
			}
		}
	}
}