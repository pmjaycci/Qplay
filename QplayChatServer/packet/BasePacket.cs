using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Packet
{
    #region WebServer
    public class RequestPacket
    {
        public string? Name;
    }
    public class ResponsePacket
    {
        public int MessageCode;
        public string? Message;
    }
    #endregion

    #region ChatServer
    public class BasePacket
    {
        public int Opcode;
        public string? Message;
    }
    #endregion

}