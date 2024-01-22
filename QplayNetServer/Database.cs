using System.Data;
using System.Reflection;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Security;

public class Database
{
    private static Database? instance;


    public IDbConnection LoginDB;
    private Database()
    {
        // 연결 문자열에 커넥션 풀링을 사용하도록 설정
        var connectionString = "Server=13.125.254.231;Port=3306;Database=login_db;Uid=root;Pwd=jaycci1@;Pooling=true;";

        // MySqlConnection 대신 IDbConnection 인터페이스를 사용하여 다른 데이터베이스에도 유연하게 대응할 수 있습니다.
        LoginDB = new MySqlConnection(connectionString);
    }

    public static Database GetInstance()
    {
        if (instance == null)
        {
            instance = new Database();
        }
        return instance;
    }
    public async Task DatabaseConnect(string connectDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (connectDB)
        {
            case "LoginDB":
                connection = (MySqlConnection)LoginDB;
                break;
        }

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
            Console.WriteLine($"ConnectDB:{connectDB} Connect!!");
        }
    }

    public async Task<MySqlDataReader> Query(string sql, string openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case "LoginDB":
                connection = (MySqlConnection)LoginDB;
                break;
        }

        //await connection.OpenAsync();

        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            var reader = cmd.ExecuteReaderAsync();
            return (MySqlDataReader)await reader;
        }
    }
    /*
    public void ExecuteQuery(string sql, string openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        
        switch(openDB)
        {
            case "LoginDB":
                connection = LoginDB;
            break;
        }


        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            int rowsAffected = cmd.ExecuteNonQuery();

            Console.WriteLine($"Rows affected: {rowsAffected}");
        }
    }
    */
}