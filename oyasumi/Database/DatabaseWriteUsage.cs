using System;
using System.Collections.Generic;

namespace oyasumi.Database
{
    // Thanks to Mempy https://github.com/osuAkatsuki/Sora/blob/master/src/Sora/Database/DatabaseWriteUsage.cs
    // Modified version of https://github.com/ppy/osu/blob/master/osu.Game/Database/DatabaseWriteUsage.cs under MIT License!
    public class DatabaseWriteUsage : IDisposable
    {
        public readonly OyasumiDbContext Context;
        private readonly Action<DatabaseWriteUsage> usageCompleted;
        public List<Exception> Errors = new List<Exception>();

        private bool isDisposed;

        /// <summary>
        ///     Whether this write usage will commit a transaction on completion.
        ///     If false, there is a parent usage responsible for transaction commit.
        /// </summary>
        public bool IsTransactionLeader = false;

        public DatabaseWriteUsage(OyasumiDbContext context, Action<DatabaseWriteUsage> onCompleted)
        {
            Context = context;
            usageCompleted = onCompleted;
        }

        public bool PerformedWrite { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            try
            {
                PerformedWrite |= Context.SaveChanges() > 0;
            }
            catch (Exception e)
            {
                Errors.Add(e);
                throw;
            }
            finally
            {
                usageCompleted?.Invoke(this);
            }
        }

        ~DatabaseWriteUsage()
        {
            Dispose(false);
        }
    }
}