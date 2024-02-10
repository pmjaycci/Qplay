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
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }
        public Dictionary<int, bool>? Items { get; set; }
        public Dictionary<int, CreatedRoomInfo>? CreatedRoomsInfo { get; set; }
        public List<LobbyUserInfo>? LobbyUsersInfo { get; set; }
    }
    public class Room : Packet
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
        public Dictionary<int, JoinRoomUserInfo>? JoinRoomUsersInfo { get; set; }
    }

    public class ExitRoom : Packet
    {
        public int State { get; set; }
        public Dictionary<int, CreatedRoomInfo>? CreatedRoomsInfo { get; set; }
        public List<LobbyUserInfo>? LobbyUsersInfo { get; set; }
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