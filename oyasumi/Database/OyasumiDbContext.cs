using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using oyasumi.Database.Models;
using oyasumi.Objects;

namespace oyasumi.Database
{
    public class OyasumiDbContext : DbContext
    {
        public OyasumiDbContext(DbContextOptions<OyasumiDbContext> options) : base(options) 
        {
            Database.EnsureCreated();
        } 

        public void Migrate()
        {
            Database.Migrate();
        }

        public DbSet<DbScore> Scores { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserStats> UsersStats { get; set; }
        public DbSet<DbBeatmap> Beatmaps { get; set; }
        public DbSet<DbChannel> Channels { get; set; }
    }
}