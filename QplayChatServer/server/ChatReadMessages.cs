using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Util;

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
        //-- 클라이언트 메시지 응답 처리
        public async Task<ChatBase.Packet> ReadMessage(TcpClient client, ChatBase.Packet request)
        {
            var response = new ChatBase.Packet();
            int opcode = request!.Opcode;
            await Task.Run(() =>
            {
                switch (opcode)
                {
                    case (int)Opcode.JoinGame:
                        {
                            var joinGame = JsonConvert.DeserializeObject<ChatRequest.JoinGame>(request.Message!);
                            string? userName = joinGame!.UserName;

                            var clients = ServerManager.GetInstance().Clients;
                            Console.WriteLine($"ConnectClient Size: {clients.Count}");
                            if (!clients.ContainsKey(userName!))
                            {

                                clients.TryAdd(userName!, client);
                                Console.WriteLine($"Add ConnectClient!! Current Clients Size: {clients.Count}");
                            }
                            else
                            {
                                clients[userName!] = client;
                            }

                            foreach (var t in clients)
                            {
                                Console.WriteLine($"ConnectUsers:: UserName : {t.Key}");
                            }

                            var packet = new ChatResponse.Packet();
                            packet!.MessageCode = (int)MessageCode.Success;
                            packet.Message = "Success";

                            response!.Opcode = (int)Opcode.Message;
                            response!.Message = JsonConvert.SerializeObject(packet);
                        }
                        break;
                    case (int)Opcode.Chat:
                        {
                            var messages = ServerManager.GetInstance().ChatMessages;
                            messages!.Enqueue(request);
                            //-- 메시지가 도착할 때마다 SemaphoreSlim을 Release하여 대기 상태에서 깨움
                            ServerManager.GetInstance().ChatSemaphore.Release();

                            var packet = new ChatResponse.Packet();
                            packet!.MessageCode = (int)MessageCode.Success;
                            packet.Message = "Success";

                            response!.Opcode = (int)Opcode.Message;
                            response!.Message = JsonConvert.SerializeObject(packet);
                        }
                        break;
                    default:
                        {
                            var packet = new ChatResponse.Packet();
                            packet!.MessageCode = (int)MessageCode.BadRequest;
                            packet.Message = "Bad Request";

                            response!.Opcode = opcode;
                            response.Message = JsonConvert.SerializeObject(packet);
                            break;
                        }
                }
            });
            return response;

        }


        //-- 호출 유저 상태에 따른 메시지 전달받을 유저 클라이언트 가져오기
        public async Task<ConcurrentQueue<TcpClient>> GetUserClients(ChatBase.Packet request)
        {
            var result = new ConcurrentQueue<TcpClient>();
            var clients = ServerManager.GetInstance().Clients;
            var users = ServerManager.GetInstance().Users;

            await Task.Run(() =>
            {
                switch (request.Opcode)
                {
                    case (int)Opcode.Chat:
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.Chat>(request.Message!);
                            var chatUser = users[packet!.UserName!];
                            foreach (var user in users)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;

                                int userState = user.Value.State;
                                string userName = user.Value.UserName!;
                                //-- 호출한 유저의 상태와 동일한 상태를 가진 유저인지?
                                if (userState == chatUser.State)
                                {
                                    switch (userState)
                                    {
                                        case (int)UserState.Room:
                                            int roomNumber = user.Value.RoomNumber;
                                            if (roomNumber == chatUser.RoomNumber)
                                            {
                                                result.Enqueue(clients[userName]);
                                            }
                                            break;

                                        default:
                                            result.Enqueue(clients[userName]);
                                            break;
                                    }
                                }
                            }
                        }

                        break;
                    case (int)Opcode.AddUserLobbyMember: //-- 본인을 제외한 로비에 위치한 유저들 가져옴
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.AddUserLobbyMember>(request.Message!);
                            foreach (var user in users)
                            {
                                var currentUser = user.Value;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (user.Value.UserName == packet!.UserName) continue;
                                if (user.Value.State != (int)UserState.Lobby) continue;
                                result.Enqueue(clients[user.Value.UserName!]);
                            }
                        }
                        break;
                    case (int)Opcode.AddChatRoomLobbyMember: //-- 본인을 제외한 로비에 위치한 유저들 가져옴
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.AddChatRoomLobbyMember>(request.Message!);
                            foreach (var user in users)
                            {
                                var currentUser = user.Value;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (user.Value.UserName == packet!.UserName) continue;
                                if (user.Value.State != (int)UserState.Lobby) continue;
                                result.Enqueue(clients[user.Value.UserName!]);
                            }
                        }
                        break;
                    case (int)Opcode.RoomLobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.RoomLobbyMember>(request.Message!);
                            foreach (var user in users)
                            {
                                var currentUser = user.Value;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (user.Value.UserName == packet!.UserName) continue;
                                if (user.Value.State != (int)UserState.Lobby) continue;
                                result.Enqueue(clients[user.Value.UserName!]);
                            }
                        }
                        break;
                    case (int)Opcode.LobbyMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.LobbyMember>(request.Message!);
                            foreach (var user in users)
                            {
                                var currentUser = user.Value;

                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (user.Value.UserName == packet!.UserName) continue;
                                if (user.Value.State != (int)UserState.Lobby) continue;
                                result.Enqueue(clients[user.Value.UserName!]);
                            }
                        }
                        break;
                    case (int)Opcode.JoinRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.JoinRoomMember>(request.Message!);

                            foreach (var user in users)
                            {
                                var currentUser = user.Value;
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (currentUser.UserName == packet!.UserName) continue;
                                if (currentUser.State == (int)UserState.Room && currentUser.RoomNumber == packet.RoomNumber)
                                {
                                    result.Enqueue(clients[user.Value.UserName!]);
                                }
                            }
                        }
                        break;
                    case (int)Opcode.ExitRoomMember:
                        {
                            var packet = JsonConvert.DeserializeObject<ChatBase.ExitRoomMember>(request.Message!);
                            foreach (var user in users)
                            {
                                //-- 전달받을 유저의 클라이언트가 없을경우
                                if (!clients.ContainsKey(user.Key)) continue;
                                if (user.Value.UserName == packet!.UserName) continue;
                                if (user.Value.State == (int)UserState.Room && user.Value.RoomNumber == packet.RoomNumber)
                                {
                                    result.Enqueue(clients[user.Value.UserName!]);
                                }
                            }
                        }
                        break;
                }

            });

            return result;
        }




    }
}