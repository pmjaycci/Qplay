using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Packet;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace QplayChatServer.server
{
    public class WebServer
    {
        public Task RunHttpServer(CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateBuilder();

            // SwaggerGen 서비스 등록 수정
            builder.Services.AddSwaggerGen(ConfigureSwagger);

            var app = builder.Build();

            app.MapGet("/", HandleHttpGetRequest);
            app.MapPost("/", HandleHttpPostRequest);
            // Swagger 설정 추가
            app.UseSwagger();
            app.UseSwaggerUI(ConfigureSwaggerUI);

            // Task 반환 추가
            return app.RunAsync(cancellationToken);
        }

        // ConfigureSwagger 메서드 추가
        static void ConfigureSwagger(SwaggerGenOptions options)
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Qplay Http Server", Version = "v1" });
        }

        // ConfigureSwaggerUI 메서드 추가
        static void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "내 API");
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
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                string requestBody = await reader.ReadToEndAsync();
                switch (header)
                {
                    //TODO JoinGame~ExitShop까지 로비에 있는 유저들에게 TCP로 메시지 호출해줘야함
                    case (int)RequestHeader.JoinGame:
                        {
                            RequestPacket? request = JsonConvert.DeserializeObject<RequestPacket>(requestBody);
                            var response = await WebReadMessages.GetInstance().InsertUserData(request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.CreateRoom:
                        {
                            RequestCreateRoom? request = JsonConvert.DeserializeObject<RequestCreateRoom>(requestBody);
                            var response = await WebReadMessages.GetInstance().CreateRoom(request!.RoomName!, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.JoinRoom:
                        {
                            RequestJoinRoom? request = JsonConvert.DeserializeObject<RequestJoinRoom>(requestBody);
                            var response = await WebReadMessages.GetInstance().JoinRoom(request!.RoomNumber!, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);

                        }
                        break;
                    case (int)RequestHeader.ExitRoom:
                        {
                            RequestPacket? request = JsonConvert.DeserializeObject<RequestCreateRoom>(requestBody);
                            var response = await WebReadMessages.GetInstance().ExitRoom(request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.JoinShop:
                        {
                            RequestPacket? request = JsonConvert.DeserializeObject<RequestPacket>(requestBody);
                            var response = await WebReadMessages.GetInstance().Shop((int)UserState.Shop, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.ExitShop:
                        {
                            RequestPacket? request = JsonConvert.DeserializeObject<RequestPacket>(requestBody);
                            var response = await WebReadMessages.GetInstance().Shop((int)UserState.Lobby, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.BuyItem:
                        {
                            RequestBuyItem? request = JsonConvert.DeserializeObject<RequestBuyItem>(requestBody);
                            var response = await WebReadMessages.GetInstance().BuyItem(request!.ItemId, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    case (int)RequestHeader.ChangeCharacter:
                        //-- 아이템 장착한 아이템 적용
                        {
                            RequestChangeCharacter? request = JsonConvert.DeserializeObject<RequestChangeCharacter>(requestBody);
                            var response = await WebReadMessages.GetInstance().ChangeCharacter(request!.EquipItems!, request!.Name!);
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                    default:
                        {
                            var response = new ResponsePacket();
                            response!.MessageCode = (int)MessageCode.BadRequest;
                            response!.Message = "Bad Request!!";
                            responseJson = JsonConvert.SerializeObject(response);
                        }
                        break;
                }
            }

            return responseJson;

        }
    }
}