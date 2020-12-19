using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RippleDatabaseMerger.Enums;

namespace RippleDatabaseMerger.Database.Models
{
    [Table("users")]
    public class RippleUser
    {
        [Required]
        [Column("id")] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("username")] [Required] public string Name { get; set; }
        [Column("password_md5")] [Required] public string Password { get; set; }
        [Column("register_datetime")] [Required] public int JoinTimestamp { get; set; }
        [Column("email")] [Required] public string Email { get; set; }
        [Column("privileges")] [Required] public RipplePrivileges Privileges { get; set; }
        [Column("ssalt")] [Required] public byte[] Salt { get; set; }
    }
}