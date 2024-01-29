using System.Net.Sockets;
using Newtonsoft.Json;
using Packet;

namespace QplayChatServer.server
{
    public class ChatReadMessages
    {
        private static ChatReadMessages? instance;
        ChatReadMessages() { }

        public static ChatReadMessages GetInstance()
        {
            if (instance == null)
            {
                instance = new ChatReadMessages();
            }
            return instance;
        }

        public BasePacket ResponseMessage(TcpClient client, BasePacket request)
        {
            int opcode = request!.Opcode;

            switch (opcode)
            {
                case (int)Opcode.JoinGame:
                    {
                        string? userName = request!.Message;
                        if (!ServerManager.GetInstance().Clients.ContainsKey(userName!))
                        {
                            ServerManager.GetInstance().Clients.TryAdd(userName!, client);
                        }
                        var packet = new BasePacket();
                        packet!.Opcode = (int)Opcode.JoinGame;
                        packet!.Message = "Success";

                        return packet;
                    }
                case (int)Opcode.Chat:
                    {
                        var packet = JsonConvert.DeserializeObject<ChatPacket>(request.Message!);

                        var message = new ChatPacket();
                        message!.Type = (int)ChatType.All;
                        message!.UserName = packet!.UserName;
                        message!.Message = packet!.Message;

                        var responseMessage = JsonConvert.SerializeObject(message);
                        BasePacket response = new BasePacket();
                        response.Opcode = (int)Opcode.Chat;
                        response.Message = responseMessage;

                        return response;
                    }
                default:
                    {
                        var packet = new BasePacket();
                        packet!.Opcode = (int)Opcode.JoinGame;
                        packet!.Message = "Success";

                        return packet;
                    }
            }
        }
        public async Task<BasePacket> ReadOpcode(BasePacket request)
        {
            int opcode = request!.Opcode;
            switch (opcode)
            {
                case (int)Opcode.Chat:
                    {
                        var packet = JsonConvert.DeserializeObject<ChatPacket>(request.Message!);
                        var user = ServerManager.GetInstance().Users[packet!.UserName!];

                        var clients = await ServerManager.GetInstance().GetBroadcastUsers(packet.UserName!, user.State);
                        var message = new ChatPacket();
                        message!.Type = (int)ChatType.All;
                        message!.UserName = packet.UserName;
                        message!.Message = packet.Message;

                        var broadcastMessage = JsonConvert.SerializeObject(message);
                        return await ServerManager.GetInstance().BroadcastMessage(clients, opcode, broadcastMessage);
                    }
                default:
                    {
                        var packet = new BasePacket();
                        packet!.Opcode = (int)Opcode.JoinGame;
                        packet!.Message = "Success";

                        return packet;
                    }
            }
        }
    }
}