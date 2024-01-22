using System.Data;
using System.Reflection;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using Org.BouncyCastle.Security;

public class Database
{
    private static Database? instance;


    public IDbConnection UserDB;
    public IDbConnection TableDB;

    public struct Item
    {
        public int Id;
        public string? Name;
        public int Category;
        public int Gender;
        public string? ImgId;
    }
    public Dictionary<int, Item> ItemTable = new Dictionary<int, Item>();
    public struct ShopItem
    {
        public int Id;
        public int Price;
    }
    public Dictionary<int, ShopItem> ShopTable = new Dictionary<int, ShopItem>();

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
    public void DatabaseConnect(string connectDB)
    {
        //MySqlConnection connection = new MySqlConnection();
        switch (connectDB)
        {
            case "UserDB":
                //connection = (MySqlConnection)UserDB;
                UserDB.Open();
                break;
            case "TableDB":
                //connection = (MySqlConnection)TableDB;
                TableDB.Open();
                break;
        }

        //if (connection.State != ConnectionState.Open)
        {
            //await connection.OpenAsync();
            Console.WriteLine($"ConnectDB:{connectDB} Connect!!");
        }
    }
    #region DB 테이블 읽기 쓰기
    public async Task<MySqlDataReader> Query(string sql, string openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case "UserDB":
                connection = (MySqlConnection)UserDB;
                break;
            case "TableDB":
                connection = (MySqlConnection)TableDB;
                break;
        }

        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            var reader = cmd.ExecuteReaderAsync();
            return (MySqlDataReader)await reader;
        }
    }

    public async Task<System.Data.Common.DbDataReader> Query(string sql, Dictionary<string, object?> param, string openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case "UserDB":
                connection = (MySqlConnection)UserDB;
                break;
            case "TableDB":
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
    public async void ExcuteQuery(string sql, string openDB)
    {
        MySqlConnection connection = new MySqlConnection();
        switch (openDB)
        {
            case "UserDB":
                connection = (MySqlConnection)UserDB;
                break;
            case "TableDB":
                connection = (MySqlConnection)TableDB;
                break;
        }

        using (MySqlCommand cmd = new MySqlCommand(sql, connection))
        {
            await cmd.ExecuteNonQueryAsync();
            //return await cmd.ExecuteNonQueryAsync();
        }
    }
    #endregion
    public async Task LoadTableDatabase()
    {
        Console.WriteLine("ItemTable 캐싱 시작");

        string query = "SELECT * FROM item";

        var result = await Query(query, "TableDB");
        Item item = new Item();
        while (result.Read())
        {
            item.Id = Convert.ToInt32(result["id"]);
            item.Name = Convert.ToString(result["name"]);
            item.Category = Convert.ToInt32(result["category"]);
            item.Gender = Convert.ToInt32(result["gender"]);
            item.ImgId = Convert.ToString(result["img_id"]);

            ItemTable[item.Id] = item;
        }
        result.Close();
        Console.WriteLine("ItemTable 캐싱 완료");
        Console.WriteLine("-----------------");
        Console.WriteLine("ShopTable 캐싱 시작");
        query = "SELECT * FROM shop";
        result = await Query(query, "TableDB");
        ShopItem shopItem = new ShopItem();
        while (result.Read())
        {
            shopItem.Id = Convert.ToInt32(result["id"]);
            shopItem.Price = Convert.ToInt32(result["price"]);
            ShopTable[shopItem.Id] = shopItem;
        }
        result.Close();
        Console.WriteLine("ShopTable 캐싱 완료");
        Console.WriteLine("-----------------");


        Console.WriteLine("Item");
        foreach (var test in ItemTable.Values)
        {
            Console.WriteLine($"id: {test.Id} / name: {test.Name} / category: {test.Category} / gender: {test.Gender} / imgId: {test.ImgId}");
        }
        Console.WriteLine("-----------------");

    }
}
