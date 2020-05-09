using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using oyasumi.Database;
using oyasumi.Objects;
using System;

namespace oyasumi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Custom server for osu!\n  by Cherry, 2020");

            Global.Factory = new OyasumiDbContextFactory();
            CreateHostBuilder(args).Build().Run();


        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
