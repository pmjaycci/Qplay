using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using Util;
namespace server
{
    public class Server
    {
        private static ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();
        public async Task RunTcpServer(CancellationToken cancellationToken)
        {
            string ip = "0.0.0.0"; // 모든 네트워크 인터페이스에 바인딩
            int port = 8060;

            // TcpListener 생성 및 시작
            TcpListener tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            tcpListener.Start();
            Console.WriteLine($"Chat Tcp 서버 시작됨 IP[{ip}] PORT[{port}]");
            Console.WriteLine("----------------------------------------------------------");

            //-- 클라이언트로부터 들어온 메세지 처리
            await ListenChatMessages(tcpListener, cancellationToken);
        }

        //-- 채팅서버 클라이언트 요청 대기
        private static async Task ListenChatMessages(TcpListener tcpListener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                client = await tcpListener.AcceptTcpClientAsync();

                _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
            }
        }
        //-- 클라이언트 요청 메시지 비동기 처리
        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var ip = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Address}";
            var port = $"{((IPEndPoint)client.Client.RemoteEndPoint!).Port}";
            try
            {
                Console.WriteLine($"클라이언트 연결됨: {ip}:{port}");


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
                        Console.WriteLine("TEST:" + request);
                        var packet = JsonConvert.DeserializeObject<Chat.Packet>(request);


                        var userName = packet!.UserName;
                        var state = packet.State;
                        var roomNumber = packet.RoomNumber;
                        if (!Users.ContainsKey(userName!))
                        {
                            var newUser = new User(client, userName!, state, roomNumber);
                            Users.TryAdd(userName!, newUser);
                            ThreadPool.QueueUserWorkItem(_ => PingCheck(Users[userName!]));
                            Console.WriteLine("핑 체크!!");
                        }
                        if (packet!.Opcode == (int)Opcode.Ping)
                        {
                            Users[userName!].IsAlive = true;
                            continue;
                        }
                        var user = Users[userName!];
                        user.State = state;
                        user.RoomNumber = roomNumber;

                        await Task.Run(() => SendMessage(packet, user));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"TCP 클라이언트 연결 해제됨: {ip}:{port}");
            }
        }

        private static void SendMessage(Chat.Packet packet, User chatUser)
        {
            string message = JsonConvert.SerializeObject(packet);
            byte[] dataBytes = Encoding.UTF8.GetBytes(message);

            //byte[] dataBytes = MessagePackSerializer.Serialize(packet);
            // 데이터의 길이를 구하고 전송
            int sendDataLength = dataBytes.Length;
            byte[] byteLength = BitConverter.GetBytes(sendDataLength);

            foreach (var user in Users.Values)
            {
                var stream = user.Client!.GetStream();

                if (user.State != chatUser.State || user.RoomNumber != chatUser.RoomNumber) continue;
                Console.WriteLine("TEST::" + user.UserName);
                //-- 데이터 크기 전송
                stream.Write(byteLength, 0, byteLength.Length);
                //-- 실제 데이터 전송
                stream.Write(dataBytes, 0, dataBytes.Length);
            }
        }
        private static void PingCheck(User user)
        {
            var packet = new Chat.Packet();
            packet!.Opcode = (int)Opcode.Ping;
            packet.UserName = user.UserName;
            packet.Message = "PingPong";
            Console.WriteLine("핑 체크 시작");

            while (true)
            {
                user.IsAlive = false;
                SendPing(packet, user.Client!);
                Thread.Sleep(3000);
                if (!user.IsAlive)
                {
                    Console.WriteLine("사망");
                }
                else
                    Console.WriteLine("생존");
            }

        }
        private static void SendPing(Chat.Packet packet, TcpClient client)
        {
            string message = JsonConvert.SerializeObject(packet);
            byte[] dataBytes = Encoding.UTF8.GetBytes(message);

            //byte[] dataBytes = MessagePackSerializer.Serialize(packet);
            // 데이터의 길이를 구하고 전송
            int sendDataLength = dataBytes.Length;
            byte[] byteLength = BitConverter.GetBytes(sendDataLength);

            var stream = client.GetStream();
            //-- 데이터 크기 전송
            stream.Write(byteLength, 0, byteLength.Length);
            //-- 실제 데이터 전송
            stream.Write(dataBytes, 0, dataBytes.Length);
        }
    }

}