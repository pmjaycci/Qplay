using Util;

namespace Response
{
    public class Packet
    {
        public int MessageCode { get; set; }
        public string? Message { get; set; }
    }

    public class LoadTable : Packet
    {
        public Dictionary<int, Item>? ItemTable { get; set; }
        public Dictionary<int, ShopItem>? ShopTable { get; set; }
    }
}