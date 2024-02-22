using Util;
public class Program
{
    public static async Task Main(string[] args)
    {
        // -- ASP.Net Core 애플리케이션 빌더 객체 추가
        var builder = WebApplication.CreateBuilder(args);

        // -- 서비스 컨테이너에 MVC 서비스 추가
        builder.Services.AddControllers();

        // -- Swagger 추가 :: API문, 브라우징 서비스 추가 및 이용 목적
        builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // -- 개발환경에서만 실행
        if (app.Environment.IsDevelopment())
        {
            //app.UseSwagger();
            //app.UseSwaggerUI();
        }
        // 전역적인 미들웨어 추가
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

        // -- Http요청을 Https로 리디렉션
        app.UseHttpsRedirection();

        // -- 인증된 요청만 처리
        app.UseAuthorization();

        // -- 컨트롤러 엔드포인트 매핑 :: 라우팅된 요청을 컨트롤러 액션으로 전달
        app.MapControllers();

        Database.GetInstance().DatabaseConnect((int)DB.UserDB);
        Database.GetInstance().DatabaseConnect((int)DB.TableDB);
        await Database.GetInstance().LoadTableDatabase();

        app.Run("http://localhost:8000");
    }
}