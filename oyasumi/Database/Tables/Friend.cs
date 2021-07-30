using oyasumi.Database.Attributes;

namespace oyasumi.Database.Models
{
    [Table("Friends")]
    public class Friend
    {
        public int Id { get; set; }
        public int Friend1 { get; set; }
        public int Friend2 { get; set; }
    }
}
