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
