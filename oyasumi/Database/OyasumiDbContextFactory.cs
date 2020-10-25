using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Org.BouncyCastle.Math.EC.Rfc7748;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Database
{
    public class OyasumiDbContextFactory : IDesignTimeDbContextFactory<OyasumiDbContext>
    {
        public static OyasumiDbContextFactory Instance = new OyasumiDbContextFactory();
        public static ConcurrentBag<OyasumiDbContext> DbPool = new ConcurrentBag<OyasumiDbContext>();

        /// <summary>
        /// Gets a free DbContext or creates a new one if everyone else is busy.
        /// </summary>
        /// <returns>OyasumiDbContext</returns>
        public static OyasumiDbContext Get()
        {
            var freeDbContext = DbPool.FirstOrDefault(x => x.Database.CurrentTransaction is null);
            if (freeDbContext is not null)
                return freeDbContext;

            var dbContext = Instance.CreateDbContext(null);
            DbPool.Add(dbContext);

            return dbContext;
        }
        public OyasumiDbContext CreateDbContext(string[] args)
        {
            return new OyasumiDbContext();
        }
    }
}
