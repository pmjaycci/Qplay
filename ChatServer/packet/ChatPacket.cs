namespace Chat
{
    public class Packet
    {
        public int State { get; set; }
        public int RoomNumber { get; set; }
        public string? UserName { get; set; }
        public string? Message { get; set; }
    }
}