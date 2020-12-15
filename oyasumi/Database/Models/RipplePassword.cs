using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oyasumi.Database.Models
{
    public class RipplePassword
    {
        [Key]
        [Microsoft.Build.Framework.Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Password { get; set; }
        public byte[] Salt { get; set; }
    }
}