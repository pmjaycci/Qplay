using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Util;
namespace server
{
    public class WebServer
    {
        public async Task RunHttpServer(CancellationToken cancellationToken)
        {
            var ip = IPAddress.Any;//"0.0.0.0";
            var port = "81";
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                //options.Listen(IPAddress.Any, 81); // 포트 번호는 필요에 따라 변경 가능
                builder.WebHost.UseUrls($"http://{ip}:{port}");
            });
            builder.Host.ConfigureLogging(logging =>
            {
                // 모든 로깅 프로바이더 지우기
                logging.ClearProviders();

                // 예제: Information 레벨 이하의 로깅 메시지는 무시
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });


            // SwaggerGen 서비스 등록 수정
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.MapGet("/", HandleHttpGetRequest);
            app.MapPost("/", HandleHttpPostRequest);
            app.MapGet("/api", HandleHttpGetRequest);
            app.MapPost("/api", HandleHttpPostRequest);

            if (app.Environment.IsDevelopment())
            {
                // Swagger 설정 추가
                app.UseSwagger();
                app.UseSwaggerUI();

            }
            Console.WriteLine($"Chat Api 서버 시작됨 IP[{ip}] PORT[{port}]");
            // Task 반환 추가
            await app.RunAsync(cancellationToken);
            Console.WriteLine("Chat Api 서버 종료됨");
        }

        // ConfigureSwagger 메서드 추가
        static void ConfigureSwagger(SwaggerGenOptions options)
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Qplay Http Server", Version = "v1" });
        }

        // ConfigureSwaggerUI 메서드 추가
        static void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Qplay Http Server API v1");
        }

        // HandleHttpGetRequest 메서드 추가
        static async Task HandleHttpGetRequest(HttpContext context)
        {
            await context.Response.WriteAsync("===========");
        }
        static async Task HandleHttpPostRequest(HttpContext context)
        {
            string response = await ReadPostMessage(context);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(response);
        }

        static async Task<string> ReadPostMessage(HttpContext context)
        {
            int header = int.Parse(context.Request.Headers["MessageType"]);
            string responseJson = "";
            var headerString = "";
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                string requestBody = await reader.ReadToEndAsync();

                switch (header)
                {
                    //TODO JoinGame~ExitShop까지 로비에 있는 유저들에게 TCP로 메시지 호출해줘야함
                    case (int)RequestHeader.JoinGame:
                        headerString = "JoinGame";
                        break;
                    case (int)RequestHeader.CreateRoom:
                        headerString = "CreateRoom";
                        break;
                    case (int)RequestHeader.JoinRoom:
                        headerString = "JoinRoom";
                        break;
                    case (int)RequestHeader.ExitRoom:
                        headerString = "ExitRoom";
                        break;
                    case (int)RequestHeader.SceneChange:
                        headerString = "SceneChange";
                        break;
                    case (int)RequestHeader.BuyItem:
                        headerString = "BuyItem";
                        break;
                    case (int)RequestHeader.EquipItems:
                        headerString = "EquipItems";
                        break;
                    default:
                        headerString = "잘못된 헤더";
                        break;
                }
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine($"ChatApiRequest:: Header::{headerString}");//{requestBody}");
                switch (header)
                {
                    //TODO JoinGame~ExitShop까지 로비에 있는 유저들에게 TCP로 메시지 호출해줘야함
                    case (int)RequestHeader.JoinGame:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.Packet>(requestBody);
                            Console.WriteLine($"접속 유저명 : {request!.UserName}");
                            Console.WriteLine("----------------------------------------------------------");
                            var response = await WebReadMessages.GetInstance().JoinGame(request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.CreateRoom:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.CreateRoom>(requestBody);
                            var response = await WebReadMessages.GetInstance().CreateRoom(request!.RoomName!, request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.JoinRoom:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.JoinRoom>(requestBody);
                            var response = await WebReadMessages.GetInstance().JoinRoom(request!.RoomNumber!, request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.ExitRoom:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.Packet>(requestBody);
                            var response = await WebReadMessages.GetInstance().ExitRoom(request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.SceneChange:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.SceneChange>(requestBody);
                            var response = await WebReadMessages.GetInstance().SceneChange(request!.UserName!, request!.State);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.BuyItem:
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.BuyItem>(requestBody);
                            var response = await WebReadMessages.GetInstance().BuyItem(request!.ItemId, request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.EquipItems:
                        //-- 아이템 장착한 아이템 적용
                        {
                            var request = JsonConvert.DeserializeObject<ApiRequest.EquipItems>(requestBody);
                            var response = await WebReadMessages.GetInstance().EquipItems(request!.Items!, request!.UserName!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    default:
                        {
                            var response = new ApiResponse.Packet();
                            response!.MessageCode = (int)MessageCode.BadRequest;
                            response!.Message = "Bad Request!!";
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                }
            }
            Console.WriteLine($"ChatApiResponse:: Header:{headerString}");//\n{responseJson}");
            Console.WriteLine("----------------------------------------------------------");
            return responseJson;

        }
    }

    internal class LoggingLevelSwitch
    {
        public LoggingLevelSwitch()
        {
        }
    }
}