using System;
using System.Collections.Generic;
using System.Linq;
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
using oyasumi.Objects;

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
			var users = context.Users;

			foreach (var u in users)
				Base.UserCache.Add(u.Username, u.Id, u);

			//new OyasumiDbContext().Migrate();

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
