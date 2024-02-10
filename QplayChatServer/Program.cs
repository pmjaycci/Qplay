using QplayChatServer;
using QplayChatServer.server;
using Util;

class Program
{
    static async Task Main(string[] args)
    {

        //-- 토큰 생성
        var cts = new CancellationTokenSource();
        ServerManager.GetInstance().Token = cts.Token;
        //-- Database 연결
        var databse = Database.GetInstance();
        Console.WriteLine("-----------------");
        if ((int)MessageCode.Success != databse.DatabaseConnect((int)DB.TableDB)) return;
        if ((int)MessageCode.Success != databse.DatabaseConnect((int)DB.UserDB)) return;

        //-- DB테이블 캐싱
        await Database.GetInstance().LoadTableDatabase();

        WebServer webServer = new WebServer();
        // HTTP 서버 시작 (메인 스레드에서 실행)
        Task httpTask = webServer.RunHttpServer(cts.Token);
        ChatServer chatServer = new ChatServer();
        // TCP 서버 시작 (별도의 스레드에서 실행)
        Task tcpTask = chatServer.RunTcpServer(cts.Token);

        // 모든 작업이 완료될 때까지 대기
        await Task.WhenAll(httpTask, tcpTask);

        //Console.ReadLine(); // 프로그램 종료 방지용
        cts.Cancel();
    }
}
