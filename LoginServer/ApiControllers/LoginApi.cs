
using System.Collections.Concurrent;
using System.Text;
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
            //var sql = $"SELECT password FROM account WHERE uuid = @uuid";
            var sql = $"SELECT password, gender, model, money, inventory.item_id, inventory.is_equip FROM account LEFT JOIN inventory ON account.uuid = inventory.uuid WHERE account.uuid = @uuid";
            var param = new Dictionary<string, object?>();
            param["@uuid"] = request.Id;
            var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB);
            var response = new Response.Login();

            if (!result.HasRows)
            {
                response.MessageCode = (int)MessageCode.NotFound;
                response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                result.Close();
                return response;
            }

            response.Items = new Dictionary<int, bool>();

            while (result.Read())
            {
                string? password = Convert.ToString(result["password"]);

                if (password != request.Password)
                {
                    response.MessageCode = (int)MessageCode.BadRequest;
                    response.Message = "캐릭명 또는 비밀번호가 틀립니다.";
                    result.Close();
                    return response;
                }

                response.Gender = Convert.ToInt32(result["gender"]);
                response.Model = Convert.ToInt32(result["model"]);
                response.Money = Convert.ToInt32(result["money"]);
                bool isItemNull = result.IsDBNull(result.GetOrdinal("item_id"));
                if (!isItemNull)
                {
                    var itemId = Convert.ToInt32(result["item_id"]);
                    var isEquip = Convert.ToBoolean(result["is_equip"]);
                    response.Items![itemId] = isEquip;
                }
            }
            response.State = (int)UserState.Lobby;
            response.RoomNumber = -1;
            response.SlotNumber = -1;
            response.UserName = request.Id;

            var gameServer = new GameServer();
            var loginGameServerPacket = new Request.LoginGameServer();
            loginGameServerPacket!.UserName = response.UserName;
            loginGameServerPacket.State = response.State;
            loginGameServerPacket.RoomNumber = response.RoomNumber;
            loginGameServerPacket.SlotNumber = response.SlotNumber;
            loginGameServerPacket.Gender = response.Gender;
            loginGameServerPacket.Model = response.Model;
            loginGameServerPacket.Money = response.Money;
            loginGameServerPacket.Items = new ConcurrentDictionary<int, bool>();

            foreach (var item in response.Items!)
            {
                Console.WriteLine($"ItemId::[{item.Key}]/ isEquip [{item.Value}]");
                loginGameServerPacket.Items.TryAdd(item.Key, item.Value);
            }
            var message = await gameServer.RequestLogin(loginGameServerPacket);
            response.MessageCode = message.MessageCode;
            response.Message = message.Message;
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
            response.ItemTable = Database.GetInstance().ItemTable;

            return response;
        }
    }




    public class GameServer
    {
        public async Task<Response.Packet> RequestLogin(Request.LoginGameServer login)
        {
            // 대상 서버의 URL
            string url = "http://localhost:8070/api/login";

            // POST 요청에 보낼 데이터
            string message = JsonConvert.SerializeObject(login);
            Console.WriteLine("TEST:" + message);
            // HTTP 클라이언트 생성
            using (HttpClient httpClient = new HttpClient())
            {
                // HTTP 요청 생성
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request!.Content = new StringContent(message, Encoding.UTF8, "application/json");
                //request.Headers.Add("MessageType", ((int)RequestHeader.JoinGame).ToString());

                // HTTP 요청 보내기
                HttpResponseMessage response = await httpClient.SendAsync(request);

                // 응답 처리
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var packet = JsonConvert.DeserializeObject<Response.Packet>(responseBody);
                    return packet!;
                }
                else
                {
                    Console.WriteLine("오류 응답 코드: " + response.StatusCode);
                    return null!;
                }
            }
        }
    }

}