using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Interfaces;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi
{
	public class Base
	{
		private static Assembly Assembly;
		public static Type[] Types;

		// Rework this pls
		public static readonly ConcurrentDictionary<PacketType, MethodInfo> MethodCache = new();
		public static readonly ConcurrentDictionary<int, List<int>> FriendCache = new();
		public static readonly ConcurrentDictionary<PacketType, Action<Packet, Presence, OyasumiDbContext>> PacketImplCache = new();
		public static readonly ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<int, IStats>> UserStatsCache = new()
		{
			[LeaderboardMode.Vanilla] = new(),
			[LeaderboardMode.Relax] = new()
		};
		public static readonly ConcurrentDictionary<string, string> PasswordCache = new();
		public static readonly TwoKeyDictionary<string, int, User> UserCache = new();

		public static void Main(string[] args)
		{
			Assembly = Assembly.GetEntryAssembly();
			Types = Assembly.GetTypes();

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
				}) 
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}
}