using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Util
{
    public class User
    {
        //-- enum : UserState
        public int State;
        public int RoomNumber;
        public string? Name;
        public int Gender;
        public int Model;
        public int Money;
        public ConcurrentDictionary<int, bool>? Items;
    }

    public class Room
    {
        public string? RoomName;
        public string? LeaderName;
        public ConcurrentBag<RoomUser>? Users;
    }
    public class RoomUser
    {
        public string? Name;
        public int Gender;
        public int Model;
        public ConcurrentBag<int>? EquipItems;
    }

}
enum MessageCode
{
    Success = 200,
    Fail = 204,
    BadRequest = 400,
    NotFound = 404
}
enum Opcode
{
    JoinGame,
    Chat
}
enum ChatType
{
    Notice,
    All
}

enum RequestHeader
{
    JoinGame,
    CreateRoom,
    JoinRoom,
    ExitRoom,
    JoinShop,
    ExitShop,
    BuyItem,
    ChangeCharacter
}

enum UserState
{
    Lobby,
    Room,
    Shop,
    BeautyRoom
}