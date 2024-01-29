using System.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Packet;
namespace LoginApi
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RequestLogin request)
        {

            ResponseLogin response = await LoginCheck(request);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }

        //TODO 캐싱테이블 정보 넘겨줘야함
        private async Task<ResponseLogin> LoginCheck(RequestLogin request)
        {
            var sql = $"SELECT password, gender, model, money, last_login FROM account WHERE uuid = @uuid";
            var param = new Dictionary<string, object?>();
            param["@uuid"] = request.Id;
            var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB);
            ResponseLogin response = new ResponseLogin();
            string? password;
            if (!result.HasRows)
            {
                response.MessageCode = 100;
                response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                result.Close();
                return response;
            }
            if (result.Read())
            {
                password = Convert.ToString(result["password"]);
                if (password != request.Password)
                {
                    response.MessageCode = 100;
                    response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                    result.Close();
                    return response;
                }
                response.MessageCode = 200;
                response.Message = "로그인에 성공하였습니다.";
                response.Gender = Convert.ToInt32(result["gender"]);
                response.Model = Convert.ToInt32(result["model"]);
                response.Money = Convert.ToInt32(result["money"]);
                response.LastLogin = Convert.ToString(result["last_login"]);
            }
            result.Close();
            return response;
        }
    }
}