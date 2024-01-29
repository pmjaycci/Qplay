namespace BasePacket
{
    public class RequestPacket
    {

    }

    public class ResponsePacket
    {
        public int MessageCode { get; set; }
        public string? Message { get; set; }
    }
}

enum MessageCode
{
    Success = 200,
    Fail = 204,
    BadRequest = 400,
    NotFound = 404
}

enum UserState
{
    Lobby,
    Room,
    Shop,
    BeautyRoom
}

