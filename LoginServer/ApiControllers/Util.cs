namespace Util
{
    public class Item
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Category { get; set; }
        public int Gender { get; set; }
        public string? ImgId { get; set; }
    }

    public class ShopItem
    {
        public int Id { get; set; }
        public int Price { get; set; }
    }
    enum MessageCode
    {
        Success = 200,
        Fail = 204,
        BadRequest = 400,
        NotFound = 404
    }

    enum UserState
    {
        Lobby,
        Room,
        Shop,
        BeautyRoom
    }

    enum DB
    {
        UserDB,
        TableDB
    }
}

