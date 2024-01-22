using BasePacket;

namespace Packet
{
    public class RequestLogin : RequestPacket
    {
        public string? Id { get; set; }
        public string? Password { get; set; }
    }

    public class ResponseLogin : ResponsePacket
    {
        public int Gender { get; set; }
        public int Model { get; set; }
        public int Money { get; set; }

        public string? LastLogin { get; set; }
    }
}
