using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Packet
{

    public class ChatPacket
    {
        public string? UserName;
        public int Type;
        public string? Message;
    }
}