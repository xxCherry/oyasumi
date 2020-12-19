using Microsoft.EntityFrameworkCore;
using RippleDatabaseMerger.Database.Models;

namespace RippleDatabaseMerger.Database
{
    public class RippleDbContext : DbContext
    {
        public RippleDbContext(DbContextOptions<RippleDbContext> options) : base(options)
        {
        }
        
        public DbSet<RippleUser> Users { get; set; }
        public DbSet<RippleScore> Scores { get; set; }
        public DbSet<RippleRelaxScore> RelaxScores { get; set; }
        public DbSet<RippleBeatmap> Beatmaps { get; set; }
    }
}
