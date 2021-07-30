using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;

namespace oyasumi
{
	public class Startup
	{
		public Startup(IConfiguration configuration) =>
			Configuration = configuration;

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();

			services.Configure<FormOptions>(x =>
			{
				x.ValueLengthLimit = int.MaxValue;
				x.MultipartBodyLengthLimit = int.MaxValue;
				x.MemoryBufferThreshold = int.MaxValue;
				x.BufferBodyLengthLimit = int.MaxValue;
				x.MultipartBoundaryLengthLimit = int.MaxValue;
				x.MultipartHeadersLengthLimit = int.MaxValue;
			}
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
		{
			DbContext.Load();
			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				DbContext.Save();
			};

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			if (!Directory.Exists("./data/"))
				Directory.CreateDirectory("./data/");
			if (!Directory.Exists("./data/beatmaps/"))
				Directory.CreateDirectory("./data/beatmaps/");
			if (!Directory.Exists("./data/avatars/"))
				Directory.CreateDirectory("./data/avatars/");
			if (!Directory.Exists("./data/osr/"))
				Directory.CreateDirectory("./data/osr/");

			var vanillaStats = DbContext.VanillaStats;
			var relaxStats = DbContext.RelaxStats;

			var friends = DbContext.Friends;
			//var tokens = DbContext.Tokens;

			foreach (var v in vanillaStats)
			{
				Base.UserStatsCache[LeaderboardMode.Vanilla].TryAdd(v.Key, v.Value);
			}

			foreach (var r in relaxStats)
			{
				Base.UserStatsCache[LeaderboardMode.Relax].TryAdd(r.Key, r.Value);
			}

			foreach (var f in friends)
				Base.FriendCache.TryAdd(f.Friend1, new());

			foreach (var f in friends)
				Base.FriendCache[f.Friend1].Add(f.Friend2);

			/*foreach (var t in tokens)
				Base.TokenCache.Add(t.UserToken, t.UserId, t);*/

			ChannelManager.Channels.TryAdd("#osu", new ("#osu", "Default osu! channel", 1, true));

			foreach (var chan in DbContext.Channels)
				ChannelManager.Channels.TryAdd(chan.Name, new (chan.Name, chan.Topic, 1, chan.Public));

			var bot = new Presence(int.MaxValue, "oyasumi", 0, 0f, 0, 0, 0, 0)
			{
				Status =
				{
					Status = ActionStatuses.Watching, 
					StatusText = "for sneaky gamers"
				}
			};


			PresenceManager.Add(bot);
			// TODO: remove this lol
			// TODO: Replace by something better than Thread()
			// Disconnect inactive users and other stuff
			/*new Thread(() =>
			{
				while (true)
				{
					if (Base.BeatmapDbStatusUpdate.Any())
					{
						while (Base.BeatmapDbStatusUpdate.TryDequeue(out var item))
						{
							if (item.IsSet)
							{
								var result = item;
								var beatmaps = _context.Beatmaps
									.AsEnumerable()
									.Where(x => result.Beatmap.SetId == x.BeatmapSetId);

								foreach (var b in beatmaps)
									b.Status = item.Beatmap.Status;
							}
							else
							{
								var result = item;
								_context.Beatmaps
									.FirstOrDefault(x => x.BeatmapId == result.Beatmap.Id)
									.Status = item.Beatmap.Status;
							}
						}

						_context.SaveChanges();
					}

					if (Base.UserDbUpdate.Count > 0)
					{
						while (Base.UserDbUpdate.TryDequeue(out var u))
						{
							var user = _context.Users.FirstOrDefault(x => x.Id == u.Id);
							user.Password = u.Password;
						}

						_context.SaveChanges();
					}

					var currentTime = Time.CurrentUnixTimestamp;
					var presences = PresenceManager.Presences.Values;

					if (!presences.Any()) continue;

					foreach (var pr in presences)
					{
						if (pr.Username == "oyasumi") continue;
						if (currentTime - pr.LastPing < 60 || pr.LastPing == 0)
							continue;

						PresenceManager.Remove(pr);

						Console.WriteLine($"Remove {pr.Username}");
					}
					Thread.Sleep(30);
				}
			}).Start();
			*/
			//app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}