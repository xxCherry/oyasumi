using Microsoft.EntityFrameworkCore;

namespace RippleDatabaseMerger.Models
{
    public class RippleDbContext : DbContext
    {
        public RippleDbContext(DbContextOptions<RippleDbContext> options) : base(options)
        {
        }
        
        
    }
}
