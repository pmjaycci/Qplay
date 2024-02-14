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
    public class User
    {
        //-- enum : UserState
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public int SlotNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }
        public ConcurrentDictionary<int, bool>? Items { get; set; }
    }
    public class LoginUser
    {
        //-- enum : UserState
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
    }

    public class Character
    {
        public int SlotNumber { get; set; }
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public List<int>? Items { get; set; }
    }
    /*
    public class JoinRoomInfo
    {
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
        //public ConcurrentDictionary<int, JoinRoomUserInfo>? JoinRoomUsersInfo { get; set; }
    }
    public class JoinRoomUserInfo
    {
        public string? UserName { get; set; }
        public int Gender { get; set; }
        public int Model { get; set; }
        public ConcurrentBag<int>? EquipItems { get; set; }
    }
    

    */
    public class Room
    {
        public int RoomNumber { get; set; }
        public int CurrentMember { get; set; }
        public string? RoomName { get; set; }
        public string? OwnerName { get; set; }
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
        Logout,
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