using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using osu.Game.IO.Legacy;
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
	public class BanchoController : Controller
	{
		private readonly OyasumiDbContext _context;

		public BanchoController(OyasumiDbContext context) =>
			_context = context;

		public async Task<FileContentResult> WrongCredentials()
		{
			Response.Headers["cho-token"] = "no-token";
			return File(await BanchoPacketLayouts.LoginReplyAsync(LoginReplies.WrongCredentials),
				"application/octet-stream");
		}

		public async Task<FileContentResult> Notification(string message)
		{
			Response.Headers["cho-token"] = "no-token";
			return File(await BanchoPacketLayouts.NotificationAsync(message),
				"application/octet-stream");
		}

		public async Task<FileContentResult> BannedError()
		{
			Response.Headers["cho-token"] = "no-token";
			return File(await BanchoPacketLayouts.BannedError(), "application/octet-stream");
		}

		public async Task<FileContentResult> AlreadyLoggedInError()
		{
			Response.Headers["cho-token"] = "no-token";
			return File(await BanchoPacketLayouts.AlreadyLoggedInError(), "application/octet-stream");
		}

		[Route("/")]
		public async Task<IActionResult> Index([FromHeader(Name = "osu-token")] string token)
		{
			if (Request.Method == "GET")
				return Ok("oyasumi - the osu! server.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			byte[] bytes = null;
			Presence presence = null;
			
			if (string.IsNullOrEmpty(token))
			{
				var (username, password, osuVersion, timezone) = await Request.Body.ParseLoginDataAsync();

				var dbUser = Base.UserCache[username];

				if (dbUser == default)
				{
					dbUser = await _context.Users.AsAsyncEnumerable().FirstOrDefaultAsync(x => x.Username == username);

					if (dbUser is null)
						return await WrongCredentials();

					Base.UserCache.Add(username, dbUser.Id, dbUser);
				}

				if (!Base.PasswordCache.TryGetValue(password, out _))
				{
					if (dbUser.Password.Length == 0)
					{
						var ripplePassword = await _context.RipplePasswords
							.AsAsyncEnumerable()
							.FirstOrDefaultAsync(x => x.UserId == dbUser.Id);

						// in case if we have ripple password (which is scrypt for Astellia (don't ask why)),
						// we need to merge it to bcrypt
						if (!Crypto.VerifySCryptPassword(ripplePassword.Password, password, ripplePassword.Salt, true))
							return await WrongCredentials();

						var passwordBcrypt = Crypto.GenerateHash(password);
						var user = await _context.Users.AsAsyncEnumerable()
							.FirstOrDefaultAsync(x => x.Username == username);

						dbUser.Password = passwordBcrypt;
						user.Password = passwordBcrypt;

						await _context.SaveChangesAsync();
					}

					if (!Crypto.VerifyPassword(password, dbUser.Password))
						return await WrongCredentials();

					Base.PasswordCache.TryAdd(password, dbUser.Password);
				}

				var ip = Request.Headers["X-Real-IP"];

				if (dbUser.Country == "XX")
				{
					var user = await _context.Users.AsAsyncEnumerable()
						.FirstOrDefaultAsync(x =>
							x.Username == username); // cached user can't be used to update something
					user.Country = (await NetUtils.FetchGeoLocation(ip)).countryCode;

					dbUser.Country = user.Country;

					await _context.SaveChangesAsync();
				}

				if (dbUser.Banned())
					return await BannedError();

				var tourney = osuVersion.Contains("tourney");

				if (PresenceManager.GetPresenceByName(dbUser.Username) is not null && !tourney)
				{
					Console.WriteLine($"{dbUser.Username} already logged in");
					return await AlreadyLoggedInError();
				}

				presence = new (dbUser, timezone);
				
				await presence.GetOrUpdateUserStats(_context, LeaderboardMode.Vanilla, false);
				
				await presence.ProtocolVersion(19);
				await presence.LoginReply(presence.Id);

				presence.Tourney = tourney;

				var banchoPermissions = BanchoPermissions.Supporter;

				if ((presence.Privileges & Privileges.ManageBeatmaps) != 0)
					banchoPermissions |= BanchoPermissions.BAT;
				if ((presence.Privileges & Privileges.ManageUsers) != 0)
					banchoPermissions |= BanchoPermissions.Moderator;

				// TODO: add new privileges for it
				if (presence.Username == "Cherry")
					banchoPermissions |= BanchoPermissions.Peppy | BanchoPermissions.Tournament;

				presence.BanchoPermissions = banchoPermissions;

				await presence.Notification("Welcome to oyasumi.");

				await presence.UserPermissions(banchoPermissions);

				// Default channel
				await presence.ChatChannelListingComplete(0);
				await presence.JoinChannel("#osu");
				await presence.ChatChannelAvailable("#osu", "Default osu! channel", 1);

				// TODO: user count
				foreach (var chan in ChannelManager.Channels.Values)
					await presence.ChatChannelAvailable(chan.Name, chan.Description, (short) chan.UserCount);

				await presence.UserPresence();
				await presence.UserStats();

				foreach (var pr in PresenceManager.Presences.Values) // go for each presence
				{
					await pr.UserPresence(presence); // send us to users
					await presence.UserPresence(pr); // send users to us
				}

				Base.FriendCache.TryGetValue(presence.Id, out var friends);

				await presence.FriendList(friends?.ToArray());

				await presence.UserPresenceSingle(presence.Id);
				PresenceManager.Add(presence);

				bytes = await presence.WritePackets();

				Response.Headers["cho-token"] = presence.Token;
				Response.Headers["cho-protocol"] = "19";

				await presence.Notification("Login took: " + stopwatch.Elapsed.TotalMilliseconds + "ms");

				stopwatch.Stop();

				return File(bytes, "application/octet-stream");
			}

			presence = PresenceManager.GetPresenceByToken(Request.Headers["osu-token"]);

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
				if (!Base.PacketImplCache.TryGetValue(packet.Type, out var pItem))
				{
					var meth = Base.Types
						.SelectMany(type => type.GetMethods())
						.FirstOrDefault(m => m.GetCustomAttribute<PacketAttribute>()?.PacketType == packet.Type);

					if (meth is null)
						continue; // no handler found for this packet, skipping...

					pItem = new()
					{
						Executor = ReflectionUtils.GetExecutor(meth),
						IsDbContextRequired = meth.GetParameters().Length > 2
					};

					Base.PacketImplCache.TryAdd(packet.Type, pItem);
				}

				presence.LastPing = Time.CurrentUnixTimestamp;

				pItem.Executor.Execute(null,
					pItem.IsDbContextRequired
						? new object[] {packet, presence, _context}
						: new object[] {packet, presence});
#if PACKET_DEBUG
					Console.WriteLine($"[{presence.Username} SENT] {packet.Type} with length {packet.Data.Length}");
#endif
			}

			bytes = await presence.WritePackets();

			return File(bytes, "application/octet-stream");
		}
	}
}