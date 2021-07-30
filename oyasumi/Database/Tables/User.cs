using oyasumi.Enums;
using oyasumi.Database.Attributes;
using System;

namespace oyasumi.Database.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Privileges Privileges { get; set; }
        public DateTimeOffset JoinDate { get; set; } = DateTime.UtcNow;
        public string UsernameAka { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string UsernameSafe { get; set; }
        public string Country { get; set; }
        public string UserpageContent { get; set; }
        public bool PreferNightcore { get; set; }
    }
}