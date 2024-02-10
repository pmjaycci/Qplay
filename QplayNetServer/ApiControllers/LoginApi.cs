using System.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Util;
namespace LoginApi
{
    //TODO: 레디스 추가해서 세션처리해줘야함
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Request.Login request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            Response.Packet response = await LoginCheck(request);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }

        private async Task<Response.Packet> LoginCheck(Request.Login request)
        {
            var sql = $"SELECT password FROM account WHERE uuid = @uuid";
            var param = new Dictionary<string, object?>();
            param["@uuid"] = request.Id;
            var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB);
            Response.Packet response = new Response.Packet();

            if (!result.HasRows)
            {
                response.MessageCode = 100;
                response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                result.Close();
                return response;
            }

            while (result.Read())
            {
                string? password = Convert.ToString(result["password"]);

                if (password != request.Password)
                {
                    response.MessageCode = 100;
                    response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                    result.Close();
                    return response;
                }
                response.MessageCode = 200;
                response.Message = "로그인에 성공하였습니다.";
            }

            result.Close();
            return response;
        }
    }



    [ApiController]
    [Route("api/[controller]")]
    public class LoadTableController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] Request.LoadTable request)
        {

            Response.LoadTable response = LoadTable(request);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }

        //TODO 캐싱테이블 정보 넘겨줘야함
        private Response.LoadTable LoadTable(Request.LoadTable request)
        {
            float version = 0.1f;
            var response = new Response.LoadTable();
            Console.WriteLine($"Request Version:{request.Version} / Server Version:{version}");
            if (request.Version != version)
            {
                response.Message = "Version is Low";
                response.MessageCode = (int)MessageCode.Fail;
                return response;
            }
            response.Message = "Success";
            response.MessageCode = (int)MessageCode.Success;
            response.ShopTable = Database.GetInstance().ShopTable;
            response.ItemTable = Database.GetInstance().ItemTable;

            return response;


        }
    }

}