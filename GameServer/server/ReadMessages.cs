using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using GameInfo;
using Newtonsoft.Json;
using Util;

namespace server
{
    public class ReadMessages
    {
        //-- 호출 유저 상태에 따른 메시지 전달받을 유저 클라이언트 가져오기
        public ConcurrentQueue<TcpClient> GetUserClients(ServerPacket.Packet request)
        {
            var result = new ConcurrentQueue<TcpClient>();
            var users = ServerManager.GetInstance().Users;
            var userNames = new List<string>();
            var sendUserName = "";
            //var clients = ServerManager.GetInstance().Clients;
            string testUserNameList = "";
            string opcodeString = ServerManager.GetInstance().GetOpcodeString(request.Opcode);
            if (users.Count > 0)
            {
                foreach (var user in users.Values)
                {
                    testUserNameList += $"[{user.UserName}/State:{user.State}]";
                }
            }
            else
                testUserNameList = "[접속 유저 없음]";
            Console.WriteLine($"전체 유저 정보 옵코드[{opcodeString}]\nㄴ{testUserNameList}");

            try
            {
                switch (request.Opcode)
                {
                    case (int)Opcode.AddUserLobbyMember:
                        {

                            var packet = JsonConvert.DeserializeObject<ServerPacket.AddChatRoomLobbyMember>(request.Message!);
                            sendUserName = packet!.UserName;
                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName)
                                {
                                    Console.WriteLine($"UserName Is Request User [{user.UserName}]");
                                    continue;

                                }
                                var client = user.Client;

                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }


                                if (user.State != (int)UserState.Lobby)
                                {
                                    Console.WriteLine($"State Is Not Lobby [{user.UserName}]");
                                    continue;
                                }
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);

                            }
                            break;
                        }
                    case (int)Opcode.AddChatRoomLobbyMember: //-- 본인을 제외한 로비에 위치한 유저들 가져옴
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.AddChatRoomLobbyMember>(request.Message!);
                            sendUserName = packet!.UserName;

                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName) continue;

                                var client = user.Client;
                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);

                            }
                            break;
                        }
                    case (int)Opcode.RoomLobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.RoomLobbyMember>(request.Message!);
                            sendUserName = packet!.UserName;

                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName) continue;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);
                            }
                            break;
                        }
                    case (int)Opcode.LobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.LobbyMember>(request.Message!);
                            sendUserName = packet!.UserName;

                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName) continue;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }
                                if (user.State != (int)UserState.Lobby) continue;
                                result.Enqueue(client!);
                                userNames.Add(user.UserName!);
                            }
                            break;
                        }
                    case (int)Opcode.JoinRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.JoinRoomMember>(request.Message!);
                            sendUserName = packet!.UserName;

                            var joinUser = users[packet!.UserName!];
                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName) continue;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }
                                if (user.State == (int)UserState.Room && user.RoomNumber == joinUser.RoomNumber)
                                {
                                    result.Enqueue(client!);
                                    userNames.Add(user.UserName!);
                                }
                            }
                            break;
                        }
                    case (int)Opcode.ExitRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ServerPacket.ExitRoomMember>(request.Message!);
                            sendUserName = packet!.UserName;

                            foreach (var user in users.Values)
                            {
                                if (user.UserName == packet!.UserName) continue;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                var client = user.Client;
                                if (client == null)
                                {
                                    Console.WriteLine($"Client Is Null [{user.UserName}]");
                                    continue;
                                }
                                if (user.State == (int)UserState.Room && user.RoomNumber == packet.RoomNumber)
                                {
                                    result.Enqueue(client!);
                                    userNames.Add(user.UserName!);
                                }
                            }
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
            Console.WriteLine($"메시지 받을 유저 목록 ::보내는 유저 [{sendUserName}]\nㄴ{userNameList}");


            return result;
        }




    }
}