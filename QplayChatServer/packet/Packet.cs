using Util;

namespace Packet
{
    public class ResponseJoinGame : ResponsePacket
    {
        public User? Data;
    }

    public class ResponseRoom : ResponsePacket
    {
        public Room? Data;
    }
    public class RequestCreateRoom : RequestPacket
    {
        public string? RoomName;
    }

    public class RequestJoinRoom : RequestPacket
    {
        public int RoomNumber;
    }

    public class RequestBuyItem : RequestPacket
    {
        public int ItemId;
    }

    public class ResponseBuyItem : ResponsePacket
    {
        public int Money;
    }
    public class RequestChangeCharacter : RequestPacket
    {
        public Dictionary<int, bool>? EquipItems;
    }

    public class ResponseChangeCharacter : ResponsePacket
    {
        public Dictionary<int, bool>? Items;
    }
}