using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GameInfo;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.IO;
using Util;
using ZstdNet;

namespace server
{
    public class Server
    {
        public void RunTcpServer(CancellationToken cancellationToken)
        {
            string ip = "0.0.0.0"; // 모든 네트워크 인터페이스에 바인딩
            int port = 8080;

            // TcpListener 생성 및 시작
            TcpListener tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            tcpListener.Start();
            Console.WriteLine($"Chat Tcp 서버 시작됨 IP[{ip}] PORT[{port}]");
            Console.WriteLine("----------------------------------------------------------");

            //-- 클라이언트로부터 들어온 메세지 처리
            var connect = Task.Run(() => ConnectClients(tcpListener, cancellationToken));
            //-- 보내야될 메세지들을 처리
            var read = Task.Run(() => ReadGameMessages(cancellationToken));
            Task.WaitAll(connect, read);

        }

        //-- 채팅서버 클라이언트 요청 대기
        private static void ConnectClients(TcpListener tcpListener, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = tcpListener.AcceptTcpClient();
                    }
                    catch (ObjectDisposedException)
                    {
                        // AcceptTcpClientAsync()이 Dispose 된 경우에 대한 처리
                        break;
                    }

                    _ = Task.Run(() => HandleTcpClientAsync(client, cancellationToken));
                }
            }
            finally
            {
                tcpListener.Stop();
                Console.WriteLine("TCP 서버 종료됨");
            }
        }
        //-- 클라이언트 요청 메시지 비동기 처리
        private static async Task HandleTcpClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            string userName = "";
            var ip = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Address}";
            var port = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Port}";
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            try
            {

                //-- 호출 들어온 유저의 스트림
                NetworkStream stream = client.GetStream();
                byte[] lengthBytes = new byte[4];

                while (!cancellationToken.IsCancellationRequested)
                {
                    // 데이터의 길이를 읽음
                    await stream.ReadAsync(lengthBytes, 0, 4);
                    int dataLength = BitConverter.ToInt32(lengthBytes, 0);
                    // 실제 데이터를 읽음
                    byte[] buffer = new byte[dataLength];
                    int bytesRead = await stream.ReadAsync(buffer, 0, dataLength, cancellationToken);
                    //-- 유저 메시지 읽기

                    if (bytesRead > 0)
                    {
                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var packet = JsonConvert.DeserializeObject<ClientPacket.Packet>(request);
                        userName = packet!.UserName!;
                        if (!users.ContainsKey(userName)) break;
                        var user = users[userName];
                        if (packet!.Opcode == (int)Opcode.JoinGame)
                        {
                            user.Client = client;
                            Console.WriteLine($"TCP 클라이언트 연결됨: {ip}:{port} / Users Count [{users.Count}]");

                            ThreadPool.QueueUserWorkItem(_ => PingCheck(user, client));
                        }
                        if (packet.Opcode == (int)Opcode.Ping)
                        {
                            user.IsAlive = true;
                        }
                        if (packet!.Opcode == (int)Opcode.Logout)
                        {
                            userName = JsonConvert.DeserializeObject<string>(packet.UserName!)!;
                            break;
                        }
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine($"유저 접속 종료! 유저명:[{userName}]");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleTcpClientAsync] 예외 발생: {ex.Message}");
            }
            finally
            {
                var test = new ExitRoomController();
                await test.ExitRoom(userName, (int)Opcode.Logout);
                /*
                foreach (var user in users.Values)
                {
                    if (user.UserName == userName) continue;

                }
                */
                if (users.ContainsKey(userName))
                {
                    client.Dispose();

                    users.TryRemove(userName!, out _);
                }

                Console.WriteLine($"TCP 클라이언트 연결 해제됨: {ip}:{port} [{userName}] 정보 제거 Users Count [{users.Count}]");
            }
        }


        private static void ReadGameMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // SemaphoreSlim의 WaitAsync를 사용하여 대기
                var semaphore = ServerManager.GetInstance().ChatSemaphore;
                semaphore.Wait();

                var messages = ServerManager.GetInstance().ChatMessages;
                if (!messages!.IsEmpty && messages.TryDequeue(out var message))
                {
                    var readMessage = new ReadMessages();
                    var messageString = JsonConvert.SerializeObject(message);
                    Task.Run(() =>
                    {
                        var clients = readMessage.GetUserClients(message!);
                        if (clients.Count > 0)
                            SendGameMessage(clients!, message);
                    });
                }
            }
        }

        private static void SendGameMessage(ConcurrentQueue<TcpClient> clients, ServerPacket.Packet message)
        {
            while (clients.TryDequeue(out var client))
            {
                try
                {
                    //-- 호출 들어온 유저의 스트림
                    NetworkStream stream = client.GetStream();

                    var token = ServerManager.GetInstance().Token;

                    string sendMessage = JsonConvert.SerializeObject(message);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(sendMessage);

                    // 데이터의 길이를 구하고 전송
                    int sendDataLength = dataBytes.Length;
                    byte[] byteLength = BitConverter.GetBytes(sendDataLength);

                    //-- 데이터 크기 전송
                    stream.Write(byteLength, 0, byteLength.Length);
                    //-- 실제 데이터 전송
                    stream.Write(dataBytes, 0, dataBytes.Length);
                    Console.WriteLine($"SendGameMessage:{sendMessage}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SendGameMessage] 예외 발생: {ex.Message}");
                }

            }

        }
        private static void PingCheck(User user, TcpClient client)
        {
            var packet = new ServerPacket.Packet();
            packet!.Opcode = (int)Opcode.Ping;
            packet.Message = "PingPong";

            while (true)
            {
                if (user.IsAlive)
                    SendPing(packet, user, client);
                Thread.Sleep(3000);
                if (!user.IsAlive)
                {
                    break;
                }
            }

        }
        private static void SendPing(ServerPacket.Packet packet, User user, TcpClient client)
        {
            try
            {
                user.IsAlive = false;
                string message = JsonConvert.SerializeObject(packet);
                byte[] dataBytes = Encoding.UTF8.GetBytes(message);

                //byte[] dataBytes = MessagePackSerializer.Serialize(packet);
                // 데이터의 길이를 구하고 전송
                int sendDataLength = dataBytes.Length;
                byte[] byteLength = BitConverter.GetBytes(sendDataLength);

                var stream = client!.GetStream();
                //-- 데이터 크기 전송
                stream.Write(byteLength, 0, byteLength.Length);
                //-- 실제 데이터 전송
                stream.Write(dataBytes, 0, dataBytes.Length);
            }
            catch (IOException)
            {
                client!.Dispose();
            }

        }
    }

}
