namespace ApiRequest
{
    public class Packet
    {
        public string? UserName { get; set; }
    }

    public class CreateRoom : Packet
    {
        public string? RoomName { get; set; }
    }

    public class JoinRoom : Packet
    {
        public int RoomNumber { get; set; }
    }
    public class SceneChange : Packet
    {
        public int State { get; set; }
    }
    public class BuyItem : Packet
    {
        public int ItemId { get; set; }
    }

    public class EquipItems : Packet
    {
        public Dictionary<int, bool>? Items { get; set; }
    }
}