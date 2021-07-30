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
	public class BanchoController : OyasumiController
	{
		[Route("/")]
		public async Task<IActionResult> Index([FromHeader(Name = "osu-token")] string token)
		{
			if (Request.Method == "GET")
				return Ok("oyasumi - the osu! server.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Presence presence = null;
			byte[] packetBytes = null;

			// If 'osu-token' is not found in headers
			// then it's most likely login request
			if (string.IsNullOrEmpty(token))
			{
				var (username, password, osuVersion, timezone) = await Request.Body.ParseLoginDataAsync();

				var dbUser = DbContext.Users[username];

				if (dbUser is null)
					return NoTokenBytes(await BanchoPackets.LoginReplyAsync(LoginReplies.WrongCredentials));

				Base.UserCache.Add(username, dbUser.Id, dbUser);

				// Bcrypt cache, since bcrypt is designed to be slow
				// (for security reasons), we want cache it, to 
				// speed up login times and reduce CPU loads.
				if (!Base.PasswordCache.TryGetValue(password, out _))
				{
					// If password is wrong, we send 'WrongCredentials' reply
					if (!Crypto.VerifyPassword(password, dbUser.Password))
						return NoTokenBytes(await BanchoPackets.LoginReplyAsync(LoginReplies.WrongCredentials));

					// If password is right, we cache it to not check another time
					Base.PasswordCache.TryAdd(password, dbUser.Password);
				}

				var ip = Request.Headers["X-Real-IP"];

				var geoData = await NetUtils.FetchGeoLocation(ip);

				// If user country is not initialized
				// then we need to do it there
				if (dbUser.Country == "XX")
					dbUser.Country = geoData.CountryCode;

				if (dbUser.Banned())
					return NoTokenBytes(await BanchoPackets.BannedError());

				// Check for the 'tourney' word
				// in osu! version so we can detect
				// tourney client and use this in our
				// next checks
				var tourney = osuVersion.Contains("tourney", StringComparison.Ordinal);

				// If presence already on the server
				// and presence is not the tourney client
				// tell client that tries to connect
				// that we're already on the server
				if (PresenceManager.GetPresenceByName(dbUser.Username) is not null && !tourney)
				{
					Console.WriteLine($"{dbUser.Username} already logged in");
					return NoTokenBytes(await BanchoPackets.AlreadyLoggedInError());
				}

				presence = new(dbUser, timezone, geoData.Longitude, geoData.Latitude);

				presence.GetOrUpdateUserStats(LeaderboardMode.Vanilla, false);

				await presence.ProtocolVersion(19);
				await presence.LoginReply(presence.Id);

				presence.Tourney = tourney;

				var banchoPermissions = BanchoPermissions.Supporter;

				if ((presence.Privileges & Privileges.ManageBeatmaps) != 0)
					banchoPermissions |= BanchoPermissions.BAT;
				if ((presence.Privileges & Privileges.ManageUsers) != 0)
					banchoPermissions |= BanchoPermissions.Moderator;

				presence.BanchoPermissions = banchoPermissions;

				await presence.Notification("Welcome to oyasumi.");

				await presence.UserPermissions(banchoPermissions);

				// Default channel
				await presence.ChatChannelListingComplete(0);
				await presence.JoinChannel("#osu");
				await presence.ChatChannelAvailable("#osu", "Default osu! channel", 1);

				foreach (var chan in ChannelManager.Channels.Values)
					await presence.ChatChannelAvailable(chan.Name, chan.Description, (short)chan.UserCount);

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

				packetBytes = await presence.WritePackets();

				Response.Headers["cho-token"] = presence.Token;
				Response.Headers["cho-protocol"] = "19";

				await presence.Notification("Login took: " + stopwatch.Elapsed.TotalMilliseconds + "ms");

				stopwatch.Stop();

				return Bytes(packetBytes);
			}

			// If 'osu-token' is found in headers
			// then most likely its packet request
			// which means client sends packet(s)
			// Let's check if their exist in
			// our presence cache
			presence = PresenceManager.GetPresenceByToken(token);

			// If they're not, then send him
			// 'BanchoRestart' packet so we
			// can add him to the presence cache
			if (presence is null)
				return NoTokenBytes(await BanchoPackets.BanchoRestart(0));

			// If he's in cache, then lets parse request body
			// for the osu! packets, that client sent to us
			await using var ms = new MemoryStream();
			await Request.Body.CopyToAsync(ms);
			ms.Position = 0;

			var packets = PacketReader.Parse(ms);

			// Go through all packets we parsed
			foreach (var packet in packets)
			{
#if PACKET_DEBUG
					Console.WriteLine($"[{presence.Username} SENT] {packet.Type} with length {packet.Data.Length}");
#endif

				// If packet implementation isn't in our cache
				if (!Base.PacketImplCache.TryGetValue(packet.Type, out var pItem))
				{
					// Let's find method for that packet
					// by attribute using reflection
					var meth = Base.Types
						.SelectMany(type => type.GetMethods())
						.FirstOrDefault(m => m.GetCustomAttribute<PacketAttribute>()?.PacketType == packet.Type);

					// if method not found skip this packet
					// and try find implementation for another one.
					if (meth is null)
						continue; // no handler found for this packet, skipping...

					pItem = new()
					{
						Executor = ReflectionUtils.GetExecutor(meth),
						IsDbContextRequired = meth.GetParameters().Length > 2
					};

					// Add packet implementation to our cache
					Base.PacketImplCache.TryAdd(packet.Type, pItem);
				}

				// Update last time we got packet
				presence.LastPing = Time.CurrentUnixTimestamp;

				// Execute packet implementation
				pItem.Executor.Execute(null, new object[] { packet, presence });
			}

			packetBytes = await presence.WritePackets();

			return Bytes(packetBytes);
		}
	}
}