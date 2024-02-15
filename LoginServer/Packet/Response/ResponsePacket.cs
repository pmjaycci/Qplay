using Table;
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
    }

    public class Login : Packet
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int SlotNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }
        public Dictionary<int, bool>? Items { get; set; }
    }
}