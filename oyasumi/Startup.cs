using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using oyasumi.Database;
using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi
{
	public class Startup
	{
		private const int PING_TIMEOUT = 120000;
		public Startup(IConfiguration configuration) =>
			Configuration = configuration;

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContextPool<OyasumiDbContext>(optionsBuilder => optionsBuilder.UseMySql(
				$"server=localhost;database={Config.Properties.Database};user={Config.Properties.Username};password={Config.Properties.Password};"));
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
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, OyasumiDbContext context)
		{
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

			// User cache, speed up inital login by 15x
			var users = context.Users.AsNoTracking().AsEnumerable();

			var vanillaStats = context.VanillaStats.AsNoTracking().AsEnumerable();
			var relaxStats = context.RelaxStats.AsNoTracking().AsEnumerable();

			var friends = context.Friends.AsNoTracking().AsEnumerable();
			var tokens = context.Tokens.AsNoTracking().AsEnumerable();

			foreach (var u in users)
				Base.UserCache.Add(u.Username, u.Id, u);

			foreach (var v in vanillaStats)
				Base.UserStatsCache[LeaderboardMode.Vanilla].TryAdd(v.Id, v);

			foreach (var r in relaxStats)
				Base.UserStatsCache[LeaderboardMode.Relax].TryAdd(r.Id, r);

			foreach (var f in friends)
				Base.FriendCache.TryAdd(f.Friend1, new());

			foreach (var f in friends)
				Base.FriendCache[f.Friend1].Add(f.Friend2);

			foreach (var t in tokens)
				Base.TokenCache.Add(t.UserToken, t.UserId, t);

			ChannelManager.Channels.TryAdd("#osu", new Channel("#osu", "Default osu! channel", 1, true));

			foreach (var chan in context.Channels)
				ChannelManager.Channels.TryAdd(chan.Name, new Channel(chan.Name, chan.Topic, 1, chan.Public));

			var bot = new Presence(int.MaxValue, "oyasumi", 0, 0f, 0, 0, 0, 0);

			bot.Status.Status = ActionStatuses.Watching;
			bot.Status.StatusText = "for sneaky gamers";

			PresenceManager.Add(bot);
			//new OyasumiDbContext().Migrate();

			
			// Disconnect inactive users and other stuff
			new Thread(() =>
			{
				var builder = new DbContextOptionsBuilder<OyasumiDbContext>().UseMySql(
				    $"server=localhost;database={Config.Properties.Database};" +
					$"user={Config.Properties.Username};password={Config.Properties.Password};");
				
				var _context = new OyasumiDbContext(builder.Options);
				
				while (true)
				{
					if (Base.BeatmapDbStatusUpdate.Any())
					{
						while (Base.BeatmapDbStatusUpdate.TryDequeue(out var item))
						{
							if (item.IsSet)
							{
								var beatmaps = _context.Beatmaps
									.AsEnumerable()
									.Where(x => item.Beatmap.SetId == x.BeatmapSetId);

								foreach (var b in beatmaps)
									b.Status = item.Beatmap.Status;
							}
							else
							{
								_context.Beatmaps
									.FirstOrDefault(x => x.Id == item.Beatmap.Id)
									.Status = item.Beatmap.Status;
							}
						}

						_context.SaveChanges();
					}
					
					if (Base.UserDbUpdate.Any())
					{
						while (Base.UserDbUpdate.TryDequeue(out var u))
						{
							var user = _context.Users.FirstOrDefault(x => x.Id == u.Id);
							user.Privileges = u.Privileges;
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

						if (currentTime - pr.LastPing <= 60 || pr.LastPing == 0)
							continue;

						PresenceManager.Remove(pr).Wait();
						Console.WriteLine($"Remove {pr.Username}");
					}
					Thread.Sleep(30);
				}
			}).Start();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}