namespace ChatBase
{
    public class Packet
    {
        public int Opcode { get; set; }
        public string? Message { get; set; }
    }
    public class Chat
    {
        public int ChatType { get; set; }
        public string? UserName { get; set; }
        public string? Message { get; set; }
    }

    #region 로비 유저에게 보낼 패킷
    public class AddUserLobbyMember
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
    }

    public class AddChatRoomLobbyMember
    {
        public int State { get; set; }
        public string? UserName { get; set; }
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
    }

    public class RoomLobbyMember
    {
        public string? UserName { get; set; }
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
    }
    public class LobbyMember
    {
        public string? UserName { get; set; }
        public int State { get; set; }
    }
    #endregion


    #region 채팅방 유저에게 보낼 패킷
    public class JoinRoomMember
    {
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
        public int SlotNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public List<int>? EquipItems { get; set; }
    }

    public class ExitRoomMember
    {
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
        public int SlotNumber { get; set; }
        public int CurrentMember { get; set; }

    }
    #endregion
}