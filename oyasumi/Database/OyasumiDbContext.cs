using System;
using oyasumi.Objects;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace oyasumi.Database
{
    public class OyasumiDbContext : DbContext
    {
        public OyasumiDbContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql($"server=localhost;database={Config.Get().Database};user={Config.Get().Username};password={Config.Get().Password}");
        }

        public DbSet<Users> DBUsers { get; set; }
        [Table("Users")]
        public class Users
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int Privileges; // change int to enum
            public DateTimeOffset JoinDate { get; set; } = DateTime.UtcNow;
            [Required]
            public string Username { get; set; }
            public string UsernameAka { get; set; }
            public string UsernameSafe { get; set; }
            public string Country { get; set; }
            [Required]
            public string Password { get; set; }
        }

        public DbSet<UserStats> DBUserStats { get; set; }

        [Table("UserStats")]
        public class UserStats
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }

            public long TotalScoreOsu { get; set; } = 0;
            public long TotalScoreTaiko { get; set; } = 0;
            public long TotalScoreCtb { get; set; } = 0;
            public long TotalScoreMania { get; set; } = 0;

            public long RankedScoreOsu { get; set; } = 0;
            public long RankedScoreTaiko { get; set; } = 0;
            public long RankedScoreCtb { get; set; } = 0;
            public long RankedScoreMania { get; set; } = 0;

            public int PerformanceOsu { get; set; } = 0;
            public int PerformanceTaiko { get; set; } = 0;
            public int PerformanceCtb { get; set; } = 0;
            public int PerformanceMania { get; set; } = 0;

            public float AccuracyOsu { get; set; } = 0;
            public float AccuracyTaiko { get; set; } = 0;
            public float AccuracyCtb { get; set; } = 0;
            public float AccuracyMania { get; set; } = 0;

            public int PlaycountOsu { get; set; } = 0;
            public int PlaycountTaiko { get; set; } = 0;
            public int PlaycountCtb { get; set; } = 0;
            public int PlaycountMania { get; set; } = 0;
        }
    }
}