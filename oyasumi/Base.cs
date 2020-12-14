//#define NO_LOGGING

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
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Interfaces;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi
{
	public class PacketItem
	{
		public ObjectMethodExecutorCompiledFast Executor { get; set; }
		public bool IsDbContextRequired { get; set; }
	}
	
	public class CommandItem
	{
		public ObjectMethodExecutorCompiledFast Executor { get; set; }
		public bool IsPublic { get; set; }
		public int? RequiredArgs { get; set; }
		public Privileges Privileges { get; set; }
	}

	public class Base
	{
		public static Type[] Types;

		public static readonly ConcurrentDictionary<PacketType, MethodInfo> MethodCache = new();
		public static readonly ConcurrentDictionary<int, List<int>> FriendCache = new();
		public static readonly ConcurrentDictionary<PacketType, PacketItem> PacketImplCache = new();
		public static readonly ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<int, IStats>> UserStatsCache = new()
		{
			[LeaderboardMode.Vanilla] = new(),
			[LeaderboardMode.Relax] = new()
		};
		
		
		public static readonly ConcurrentDictionary<string, string> PasswordCache = new();
		public static readonly TwoKeyDictionary<string, int, User> UserCache = new();
		public static readonly ConcurrentDictionary<string, CommandItem> CommandCache = new();
		public static readonly ConcurrentQueue<Beatmap> BeatmapDbStatusUpdate = new();
		public static readonly ConcurrentQueue<User> UserDbUpdate = new();
		public static void Main(string[] args)
		{
			Types = Assembly.GetEntryAssembly().GetTypes();

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
#if NO_LOGGING
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
				})
#endif
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}
}