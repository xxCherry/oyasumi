using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using oyasumi.Database.Models;
using oyasumi.Objects;

namespace oyasumi.Database
{
    public class OyasumiDbContext : DbContext
    {
        public OyasumiDbContext()
        {
           // Database.EnsureCreated();
        }

        public OyasumiDbContext(DbContextOptionsBuilder optionsBuilder)
        {
        }
        public void Migrate()
        {
            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        {
            optionsBuilder.UseMySql(
                $"server=localhost;database={Config.Properties.Database};user={Config.Properties.Username};password={Config.Properties.Password}");
        }

        public DbSet<DbScore> Scores { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserStats> UsersStats { get; set; }
        public DbSet<DbBeatmap> Beatmaps { get; set; }
    }
}