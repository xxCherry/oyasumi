namespace oyasumi.Database.Models
{
    public class Token
    {
        public int Id { get; set; }
        public string UserToken { get; set; }
        public int UserId { get; set; }
    }
}