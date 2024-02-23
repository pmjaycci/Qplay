using Util;
using server;
using ZstdNet;

class Program
{
    static async Task Main(string[] args)
    {

        //-- 토큰 생성
        var cts = new CancellationTokenSource();
        ServerManager.GetInstance().Token = cts.Token;
        //-- Database 연결
        var databse = Database.GetInstance();
        Console.WriteLine("----------------------------------------------------------");
        if ((int)MessageCode.Success != databse.DatabaseConnect((int)DB.TableDB)) return;
        if ((int)MessageCode.Success != databse.DatabaseConnect((int)DB.UserDB)) return;

        //-- DB테이블 캐싱
        await Database.GetInstance().LoadTableDatabase();
        Console.WriteLine("----------------------------------------------------------");

        ApiServer apiServer = new ApiServer();
        var api = Task.Run(() => apiServer.RunHttpServer(cts.Token));

        Server gameServer = new Server();
        await Task.Run(() => gameServer.RunTcpServer(cts.Token));
        await api;
        cts.Cancel();
    }
}
