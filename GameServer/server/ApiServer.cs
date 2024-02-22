using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Util;
namespace server
{
    public class ApiServer
    {
        public async Task RunHttpServer(CancellationToken cancellationToken)
        {
            var ip = IPAddress.Any;//"0.0.0.0";
            var port = "8070";
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
            builder.Services.AddControllers(); // 이 부분이 누락된 부분입니다.

            var app = builder.Build();

            app.Use(async (context, next) =>
             {
                 // 요청의 content-type을 확인하고 필요에 따라 설정
                 string contentType = context.Request.Headers["Content-Type"];

                 // 예시: JSON 데이터를 기대하는 경우
                 if (contentType != null && contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                 {
                     // content-type을 설정
                     context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
                 }

                 // 다음 미들웨어로 전달
                 await next();
             });

            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                // Swagger 설정 추가
                app.UseSwagger();
                app.UseSwaggerUI();

            }
            Console.WriteLine($"Chat Api 서버 시작됨 IP[{ip}] PORT[{port}]");
            Console.WriteLine("----------------------------------------------------------");

            // Task 반환 추가
            await app.RunAsync(cancellationToken);
            Console.WriteLine("게임 Api 서버 종료됨");
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

    }
}