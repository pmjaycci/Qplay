using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using QplayChatServer.server;
using ZstdNet;

namespace QplayChatServer
{
    public class ChatServer
    {
        public async Task RunTcpServer(CancellationToken cancellationToken)
        {
            string ip = "0.0.0.0"; // 모든 네트워크 인터페이스에 바인딩
            int port = 8080;

            // TcpListener 생성 및 시작
            TcpListener tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            tcpListener.Start();
            Console.WriteLine($"Chat Tcp 서버 시작됨 IP[{ip}] PORT[{port}]");
            Console.WriteLine("----------------------------------------------------------");

            //-- 클라이언트로부터 들어온 메세지 처리
            var listenChatMessages = ListenChatMessages(tcpListener, cancellationToken);
            //-- 보내야될 메세지들을 처리
            var sendChatMessage = SendChatMessages(cancellationToken);

            await Task.WhenAll(listenChatMessages, sendChatMessage);

        }

        //-- 채팅서버 클라이언트 요청 대기
        private static async Task ListenChatMessages(TcpListener tcpListener, CancellationToken cancellationToken)
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
            try
            {
                //-- 호출 들어온 유저의 스트림
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested)
                {
                    //-- 유저 메시지 읽기
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        var ip = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Address}";
                        var port = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Port}";
                        Console.WriteLine($"TCP 클라이언트 연결됨: {ip}:{port}");

                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        var packet = JsonConvert.DeserializeObject<ChatBase.Packet>(request);

                        //-- 메시지 체크후 반환된 Opcode값에 따라 타 유저에게 메시지 전달해야되는 메시지의 경우 Queue에 담아 비동기로 처리
                        var responsePacket = await ChatReadMessages.GetInstance().ReadMessage(client, packet!);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"TCP 클라이언트 연결 해제됨: {((IPEndPoint)client.Client.RemoteEndPoint!).Address}");
                string? userName = "";

                //-- 연결 종료로 인해 캐싱되어 있는 Client의 데이터 삭제
                var serverManager = ServerManager.GetInstance();
                var clients = serverManager.Clients;
                var users = serverManager.Users;
                foreach (var disconnectClient in clients)
                {

                    if (disconnectClient.Value == client)
                    {
                        userName = disconnectClient.Key;
                        break;
                    }
                }
                if (clients.ContainsKey(userName))
                {
                    clients.TryRemove(userName, out _);
                }
                if (users.ContainsKey(userName))
                {
                    var WebReadMessages = new WebReadMessages();
                    await WebReadMessages.ExitRoom(userName);
                    users.TryRemove(userName, out _);
                    Console.WriteLine($"유저 로그아웃! 정보 제거: {userName}");
                }

                client.Close();

            }
        }


        private async Task SendChatMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // SemaphoreSlim의 WaitAsync를 사용하여 대기
                var semaphore = ServerManager.GetInstance().ChatSemaphore;
                Console.WriteLine("SendChatMessages:: 메시지 대기중");
                await semaphore.WaitAsync();
                var messages = ServerManager.GetInstance().ChatMessages;
                if (!messages!.IsEmpty && messages.TryDequeue(out var message))
                {
                    var clients = await ChatReadMessages.GetInstance().GetUserClients(message!);
                    await SendChatMessage(clients!, message);
                }
            }
        }

        private async Task SendChatMessage(ConcurrentQueue<TcpClient> clients, ChatBase.Packet message)
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