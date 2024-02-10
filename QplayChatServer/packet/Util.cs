using System.Collections.Concurrent;

namespace Table
{
    public class Item
    {
        public int Id;
        public string? Name;
        public int Category;
        public int Gender;
        public string? ImgId;
    }
    public class ShopItem
    {
        public int Id;
        public int Price;
    }
}
namespace GameInfo
{
    public class UserInfo
    {
        //-- enum : UserState
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }
        public ConcurrentDictionary<int, bool>? Items { get; set; }
    }

    public class JoinRoomInfo
    {
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
        public ConcurrentDictionary<int, JoinRoomUserInfo>? JoinRoomUsersInfo { get; set; }
    }
    public class JoinRoomUserInfo
    {
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public ConcurrentBag<int>? EquipItems { get; set; }
    }
    public class LobbyUserInfo
    {
        //-- enum : UserState
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
    }

    public class CreatedRoomInfo
    {
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
        public List<string>? RoomUsersInfo { get; set; }
    }

}

namespace Util
{
    enum MessageCode
    {
        Success = 200,
        Fail = 204,
        BadRequest = 400,
        NotFound = 404
    }

    enum Opcode
    {
        Message, //-- 기본 응답 (서버->클라이언트 Tcp메시지 호출 응답용)
        JoinGame, //-- 게임 접속 (서버<->클라이언트)
        Chat, //-- 채팅 (서버<->클라이언트)
        AddUserLobbyMember,
        AddChatRoomLobbyMember,
        RoomLobbyMember,
        LobbyMember,
        JoinRoomMember,
        ExitRoomMember,
    }
    enum RequestHeader
    {
        JoinGame,
        CreateRoom,
        JoinRoom,
        ExitRoom,
        SceneChange,
        BuyItem,
        EquipItems
    }
    enum ChatType
    {
        Notice,
        All
    }


    enum UserState
    {
        Lobby,
        Room,
        Shop,
        BeautyRoom,
        Logout
    }

    enum DB
    {
        UserDB,
        TableDB
    }

    enum Gender
    {
        Female,
        Male
    }

    enum Category
    {
        Hair,
        Cloth,
        Ears,
        Eyes,
        EyesAcc,
        Face,
        Lip,
        LipAcc,
        Neck,
        Background,
        Effect,
        Pet,
    }
}