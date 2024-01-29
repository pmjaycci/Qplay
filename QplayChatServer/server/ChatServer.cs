using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Packet;
using QplayChatServer.server;

namespace QplayChatServer
{
    public class ChatServer
    {

        private static ConcurrentQueue<BroadcastUserMessage>? Messages;
        public async Task RunTcpServer(CancellationToken cancellationToken)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 12345);
            tcpListener.Start();
            Console.WriteLine("TCP 서버 시작됨");
            Messages = new ConcurrentQueue<BroadcastUserMessage>();
            try
            {
                //-- 클라이언트로부터 들어온 메세지 처리
                await ListenMessages(tcpListener, cancellationToken);
                //-- 보내야될 메세지들을 처리
                await SendMessages();
            }
            finally
            {
                tcpListener.Stop();
                Console.WriteLine("TCP 서버 종료됨");
            }
        }
        private static async Task ListenMessages(TcpListener tcpListener, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    await HandleTcpClientAsync(client, cancellationToken);
                }
            }
            finally
            {
                tcpListener.Stop();
                Console.WriteLine("TCP 서버 종료됨");
            }
        }
        private static async Task HandleTcpClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TCP 클라이언트 연결됨: {((IPEndPoint)client.Client.RemoteEndPoint!).Address}");

            try
            {
                //-- 호출 들어온 유저의 스트림
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                BroadcastUserMessage userClient = new BroadcastUserMessage();
                while (true)
                {
                    //-- 유저 메시지 읽기
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {

                        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var packet = JsonConvert.DeserializeObject<BasePacket>(request);

                        //-- 메시지 체크후 반환된 Opcode값에 따라 타 유저에게 메시지 전달해야되는 메시지의 경우 Queue에 담아 비동기로 처리
                        BasePacket responsePacket = ChatReadMessages.GetInstance().ResponseMessage(client, packet!);
                        if (responsePacket.Opcode != (int)Opcode.JoinGame)
                        {
                            userClient.Client = client;
                            userClient.Message = packet;
                            Messages!.Enqueue(userClient);
                            NotifyMessageArrived();
                        }

                        //-- 호출 들어온 메시지의 결과 반환
                        string resposne = JsonConvert.SerializeObject(responsePacket);
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(resposne);

                        await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"TCP 클라이언트 연결 해제됨: {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
                string? user = "";

                //-- 연결 종료로 인해 캐싱되어 있는 Client의 데이터 삭제
                foreach (var disconnectClient in ServerManager.GetInstance().Clients)
                {
                    if (disconnectClient.Value == client)
                    {
                        user = disconnectClient.Key;
                        break;
                    }
                }
                if (ServerManager.GetInstance().Clients.ContainsKey(user))
                {
                    ServerManager.GetInstance().Clients.TryRemove(user, out _);
                }

                client.Close();

            }
        }
        private static SemaphoreSlim semaphore = new SemaphoreSlim(0);  // SemaphoreSlim을 사용하여 대기 상태 관리

        private static async Task SendMessages()
        {
            while (true)
            {
                // SemaphoreSlim의 WaitAsync를 사용하여 대기
                await semaphore.WaitAsync();
                if (!Messages!.IsEmpty && Messages.TryDequeue(out var message))
                {
                    await ChatReadMessages.GetInstance().ReadOpcode(message.Message!);
                }
            }
        }

        // 메시지가 도착할 때마다 SemaphoreSlim을 Release하여 대기 상태에서 깨움
        private static void NotifyMessageArrived()
        {
            semaphore.Release();
        }
    }

}