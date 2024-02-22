using GameInfo;

namespace ApiResponse
{
    public class Packet
    {
        public int MessageCode { get; set; }
        public string? Message { get; set; }
    }



    public class JoinGame : Packet
    {
        public Dictionary<int, Room>? Rooms { get; set; }
        public List<LoginUser>? LoginUsers { get; set; }
    }

    public class CreateRoom : Packet
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int SlotNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
    }
    public class JoinRoom : Packet
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int SlotNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
        public List<Character>? Characters { get; set; }
    }

    public class ExitRoom : Packet
    {
        public int State { get; set; }
        public Dictionary<int, Room>? Rooms { get; set; }
        public List<LoginUser>? LoginUsers { get; set; }
    }
    public class SceneChange : Packet
    {
        public int State { get; set; }
    }
    public class BuyItem : Packet
    {
        public int ItemId { get; set; }
        public int Money { get; set; }
    }

    public class EquipItems : Packet
    {
        public Dictionary<int, bool>? Items { get; set; }
    }
}