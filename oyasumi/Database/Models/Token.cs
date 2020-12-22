using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oyasumi.Database.Models
{
    public class Token
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column(TypeName="varchar(36)")] public string UserToken { get; set; }
        public int UserId { get; set; }
    }
}