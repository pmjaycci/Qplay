using System.Data;
using MySql.Data.MySqlClient;
using Table;
using Util;

public class Database
{
    private static Database? instance;


    public IDbConnection UserDB;
    public IDbConnection TableDB;

    public Dictionary<int, Item> ItemTable = new Dictionary<int, Item>();

    private Database()
    {
        // 연결 문자열에 커넥션 풀링을 사용하도록 설정
        var userDatabaseUrl = "Server=13.125.254.231;Port=3306;Database=user_db;Uid=root;Pwd=jaycci1@;Pooling=true;";
        var tableDatabaseUrl = "Server=13.125.254.231;Port=3306;Database=table_db;Uid=root;Pwd=jaycci1@;Pooling=true;";

        // MySqlConnection 대신 IDbConnection 인터페이스를 사용하여 다른 데이터베이스에도 유연하게 대응할 수 있습니다.
        UserDB = new MySqlConnection(userDatabaseUrl);
        TableDB = new MySqlConnection(tableDatabaseUrl);
    }

    public static Database GetInstance()
    {
        if (instance == null)
        {
            instance = new Database();
        }
        return instance;
    }
    public int DatabaseConnect(int connectDB)
    {
        //MySqlConnection connection = new MySqlConnection();
        string? connect;
        switch (connectDB)
        {
            case (int)DB.UserDB:
                UserDB.Open();
                connect = "User  DB Connect";
                break;
            case (int)DB.TableDB:
                TableDB.Open();
                connect = "Table DB Connect";
                break;
            default:
                connect = "Not Found Table";
                Console.WriteLine($"ConnectDB:{connect}");
                return (int)MessageCode.NotFound;
        }

        Console.WriteLine($"ConnectDB:{connect}");
        return (int)MessageCode.Success;
    }
    #region DB 테이블 읽기 쓰기
    public async Task<MySqlDataReader> Query(string sql, int openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case (int)DB.UserDB:
                connection = (MySqlConnection)UserDB;
                break;
            case (int)DB.TableDB:
                connection = (MySqlConnection)TableDB;
                break;
        }

        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            var reader = cmd.ExecuteReaderAsync();
            return (MySqlDataReader)await reader;
        }
    }

    public async Task<System.Data.Common.DbDataReader> Query(string sql, Dictionary<string, object?> param, int openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case (int)DB.UserDB:
                connection = (MySqlConnection)UserDB;
                break;
            case (int)DB.TableDB:
                connection = (MySqlConnection)TableDB;
                break;
        }

        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            foreach (var data in param)
            {
                cmd.Parameters.AddWithValue(data.Key, data.Value);
            }
            var reader = await cmd.ExecuteReaderAsync();
            return reader;
        }
    }
    public async Task<int> ExecuteQuery(string sql, Dictionary<string, object?> param, int openDB)
    {
        try
        {
            MySqlConnection connection = new MySqlConnection();
            switch (openDB)
            {
                case (int)DB.UserDB:
                    connection = (MySqlConnection)UserDB;
                    break;
                case (int)DB.TableDB:
                    connection = (MySqlConnection)TableDB;
                    break;
            }

            using (MySqlCommand cmd = new MySqlCommand(sql, connection))
            {
                foreach (var data in param)
                {
                    cmd.Parameters.AddWithValue(data.Key, data.Value);
                }
                await cmd.ExecuteNonQueryAsync();

            }
            return (int)MessageCode.Success;
        }
        catch
        {
            return (int)MessageCode.Fail;
        }

    }
    public async Task<int> ExecuteQueryWithTransaction(List<string> sqlQueries, List<Dictionary<string, object?>> paramList, int openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case (int)DB.UserDB:
                connection = (MySqlConnection)UserDB;
                break;
            case (int)DB.TableDB:
                connection = (MySqlConnection)TableDB;
                break;
        }

        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            for (int i = 0; i < paramList.Count; i++)
            {
                foreach (string sql in sqlQueries)
                {
                    using MySqlCommand cmd = new MySqlCommand(sql, connection, transaction);
                    foreach (var data in paramList[i])
                    {
                        cmd.Parameters.AddWithValue(data.Key, data.Value);
                    }
                    await cmd.ExecuteNonQueryAsync();
                }
            }


            await transaction.CommitAsync();
            return (int)MessageCode.Success;
        }
        catch
        {
            await transaction.RollbackAsync();
            return (int)MessageCode.Fail;
        }
    }
    #endregion
    public async Task LoadTableDatabase()
    {
        Console.WriteLine("ItemTable 캐싱 시작");

        string query = "SELECT * FROM item";

        var result = await Query(query, (int)DB.TableDB);
        while (result.Read())
        {
            Item item = new Item();
            item!.Id = Convert.ToInt32(result["id"]);
            item.Name = Convert.ToString(result["name"]);
            item.Category = Convert.ToInt32(result["category"]);
            item.Gender = Convert.ToInt32(result["gender"]);
            item.ImgId = Convert.ToString(result["img_id"]);
            item.Price = Convert.ToInt32(result["price"]);
            ItemTable[item.Id] = item;
        }
        result.Close();
        Console.WriteLine("ItemTable 캐싱 완료");
        Console.WriteLine("-----------------");
    }
}
