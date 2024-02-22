using Util;
using server;

class Program
{
    static async Task Main(string[] args)
    {
        //-- 토큰 생성
        var cts = new CancellationTokenSource();

        Server chatServer = new Server();
        await Task.Run(() => chatServer.RunTcpServer(cts.Token));

        //Console.ReadLine(); // 프로그램 종료 방지용
        cts.Cancel();
    }
}
