using oyasumi.Database.Attributes;

namespace oyasumi.Database.Models
{
    [Table("Channels")]
    public class DbChannel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public bool Public { get; set; }
    }
}
