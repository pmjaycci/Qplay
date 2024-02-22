using System.Collections.Concurrent;

namespace Request
{
    public class LoadTable
    {
        public float Version { get; set; }
    }
    public class Login
    {
        public string? Id { get; set; }
        public string? Password { get; set; }
    }

    public class LoginGameServer
    {
        public string? UserName { get; set; }
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int SlotNumber { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }
        public ConcurrentDictionary<int, bool>? Items { get; set; }
    }
}