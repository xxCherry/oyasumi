using MySql.Data.MySqlClient;

namespace oyasumi.Database
{
    public class MySqlProvider
    {
        public static MySqlConnection GetDbConnection() => new (Base.ConnectionString);
    }
}