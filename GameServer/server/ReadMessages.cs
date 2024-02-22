using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Util;

namespace server
{
    public class ReadMessages
    {
        //-- 호출 유저 상태에 따른 메시지 전달받을 유저 클라이언트 가져오기
        public async Task<ConcurrentQueue<TcpClient>> GetUserClients(ServerPacket.Packet request)
        {
            var result = new ConcurrentQueue<TcpClient>();
            var users = ServerManager.GetInstance().Users;
            var userNames = new List<string>();
            await Task.Run(() =>
            {
                switch (request.Opcode)
                {
                    case (int)Opcode.AddChatRoomLobbyMember: //-- 본인을 제외한 로비에 위치한 유저들 가져옴
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.AddChatRoomLobbyMember>(request.Message!);
                            foreach (var user in users.Values)
                            {
                                var client = user.Client;
                                if (user.Client == null) continue;
                                if (user.UserName == packet!.UserName) continue;
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);

                            }
                        }
                        break;
                    case (int)Opcode.RoomLobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.RoomLobbyMember>(request.Message!);
                            foreach (var user in users.Values)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (user.Client == null) continue;
                                if (user.UserName == packet!.UserName) continue;
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);
                            }
                        }
                        break;
                    case (int)Opcode.LobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.LobbyMember>(request.Message!);
                            foreach (var user in users.Values)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (user.Client == null) continue;
                                if (user.UserName == packet!.UserName) continue;
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);
                            }
                        }
                        break;
                    case (int)Opcode.JoinRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.JoinRoomMember>(request.Message!);
                            var joinUser = users[packet!.UserName!];
                            foreach (var user in users.Values)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (user.Client == null) continue;
                                if (user.UserName == packet!.UserName) continue;
                                if (user.State == (int)UserState.Room && user.RoomNumber == joinUser.RoomNumber)
                                {
                                    result.Enqueue(client!);
                                    userNames.Add(user.UserName!);
                                }
                            }
                        }
                        break;
                    case (int)Opcode.ExitRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.ExitRoomMember>(request.Message!);
                            foreach (var user in users.Values)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (user.Client == null) continue;
                                if (user.UserName == packet!.UserName) continue;
                                if (user.State == (int)UserState.Room && user.RoomNumber == packet.RoomNumber)
                                {
                                    result.Enqueue(client!);
                                    userNames.Add(user.UserName!);
                                }
                            }
                        }
                        break;
                }
            });
            string userNameList = "";
            if (userNames.Count > 0)
            {
                foreach (var userName in userNames)
                {
                    userNameList += $"[{userName}]";
                }
            }
            else
            {
                userNameList = "[없음]";
            }
            Console.WriteLine($"GetUserClients ::\n메시지 받을 유저 목록\n{userNameList}");
            return result;
        }




    }
}