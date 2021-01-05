using Microsoft.EntityFrameworkCore;
using gulagDatabaseMerger.Database.Models;

namespace gulagDatabaseMerger.Database
{
    public class GulagDbContext : DbContext
    {
        public GulagDbContext(DbContextOptions<GulagDbContext> options) : base(options)
        {
        }
        
        public DbSet<GulagUser> Users { get; set; }
        public DbSet<GulagStats> Stats { get; set; }
        public DbSet<GulagScore> Scores { get; set; }
        public DbSet<GulagRelaxScore> RelaxScores { get; set; }
        public DbSet<GulagBeatmap> Beatmaps { get; set; }
    }
}
