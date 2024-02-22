using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
            var sendMessages = Task.Run(() => SendMessages(cancellationToken));
            Task.WaitAll(connect, sendMessages);
        }

        //-- 채팅서버 클라이언트 요청 대기
        private static async Task ConnectClients(TcpListener tcpListener, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await tcpListener.AcceptTcpClientAsync();
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
            try
            {
                Console.WriteLine($"TCP 클라이언트 연결됨: {ip}:{port}");

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
                        var users = ServerManager.GetInstance().Users;
                        userName = packet!.UserName!;
                        if (!users.ContainsKey(userName)) break;
                        var user = users[userName];
                        if (user.Client == null) user.Client = client;
                        if (packet!.Opcode == (int)Opcode.Logout)
                        {
                            userName = JsonConvert.DeserializeObject<string>(packet.UserName!)!;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
            }
            finally
            {
                var serverManager = ServerManager.GetInstance();
                var users = serverManager.Users;
                /*
                foreach (var user in users.Values)
                {
                    if (user.UserName == userName) continue;

                }
                */
                if (users.ContainsKey(userName))
                {
                    client.Close();
                    users.TryRemove(userName!, out _);
                    Console.WriteLine($"유저 로그아웃! 정보 제거: {userName}");
                }

                Console.WriteLine($"TCP 클라이언트 연결 해제됨: {ip}:{port}");
            }
        }


        private async Task SendMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // SemaphoreSlim의 WaitAsync를 사용하여 대기
                var semaphore = ServerManager.GetInstance().ChatSemaphore;
                Console.WriteLine("SendMessages:: 메시지 대기중");
                await semaphore.WaitAsync();
                var messages = ServerManager.GetInstance().ChatMessages;
                if (!messages!.IsEmpty && messages.TryDequeue(out var message))
                {
                    var readMessage = new ReadMessages();
                    var clients = await readMessage.GetUserClients(message!);
                    await SendMessage(clients!, message);
                }
            }
        }

        private async Task SendMessage(ConcurrentQueue<TcpClient> clients, ServerPacket.Packet message)
        {
            while (clients.TryDequeue(out var client))
            {
                try
                {
                    //-- 호출 들어온 유저의 스트림
                    NetworkStream stream = client.GetStream();

                    var token = ServerManager.GetInstance().Token;

                    string sendMessage = JsonConvert.SerializeObject(message);
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(sendMessage);

                    await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length, token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"예외 발생: {ex.Message}");
                }
            }

        }


    }

}