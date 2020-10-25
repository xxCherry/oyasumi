using oyasumi.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oyasumi.Database.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Privileges Privileges { get; set; }
        public DateTimeOffset JoinDate { get; set; } = DateTime.UtcNow;
        [Required]
        public string Username { get; set; }
        public string UsernameAka { get; set; }
        public string UsernameSafe { get; set; }
        public string Country { get; set; }
        [Required]
        public string Password { get; set; }
    }
}