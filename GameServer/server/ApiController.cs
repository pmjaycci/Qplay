using System.Collections.Concurrent;
using Newtonsoft.Json;
using Util;
using GameInfo;
using ZstdNet;
using Ubiety.Dns.Core;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace server
{
    //TODO: 레디스 추가해서 세션처리해줘야함
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.Login request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine("여기들어옴~~~~~~" + testData);

            ApiResponse.Packet response = await Login(request);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.Packet> Login(ApiRequest.Login request)
        {
            var response = new ApiResponse.Packet();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            var userName = request.UserName!;

            await Task.Run(() =>
            {
                if (!users.ContainsKey(userName))
                {
                    User user = new User();
                    user!.UserName = request.UserName;
                    user.State = request.State;
                    user.RoomNumber = request.RoomNumber;
                    user.SlotNumber = request.SlotNumber;
                    user.Gender = request.Gender;
                    user.Model = request.Model;
                    user.Money = request.Money;
                    user.Items = request.Items;
                    users.TryAdd(userName, user);
                }
                else
                {
                    var user = users[userName];
                    user!.UserName = request.UserName;
                    user.State = request.State;
                    user.RoomNumber = request.RoomNumber;
                    user.SlotNumber = request.SlotNumber;
                    user.Gender = request.Gender;
                    user.Model = request.Model;
                    user.Money = request.Money;
                    user.Items = request.Items;
                }

            });

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "성공적으로 로그인 하였습니다.";
            return response;
        }

    }

    //TODO: 레디스 추가해서 세션처리해줘야함
    [ApiController]
    [Route("api/[controller]")]
    public class JoinGameController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.Packet request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.JoinGame response = await JoinGame(request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.JoinGame> JoinGame(string userName)
        {
            var response = new ApiResponse.JoinGame();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;


            response!.Rooms = new Dictionary<int, Room>();
            var rooms = serverManager.Rooms;
            await Task.Run(() =>
            {
                //-- 생성된 방 정보 받기
                foreach (var roomInfo in rooms)
                {
                    var room = roomInfo.Value;
                    if (room.CurrentMember <= 0) continue; //-- 현재인원이 0명이하일 경우 건너뛰기

                    int createdRoomNumber = roomInfo.Key;
                    var createdRoomInfo = new Room();
                    createdRoomInfo!.RoomNumber = createdRoomNumber;
                    createdRoomInfo.CurrentMember = roomInfo.Value.CurrentMember;
                    createdRoomInfo.RoomName = roomInfo.Value.RoomName;
                    createdRoomInfo.OwnerName = roomInfo.Value.OwnerName;

                    response.Rooms[roomInfo.Key] = createdRoomInfo;
                }

                //-- 접속중인 유저들 정보 받기
                response.LoginUsers = new List<LoginUser>();

                foreach (var info in users)
                {
                    var user = info.Value;
                    var loginUser = new LoginUser();
                    loginUser!.UserName = user.UserName;
                    loginUser.State = user.State;
                    loginUser.RoomNumber = user.RoomNumber;

                    response.LoginUsers.Add(loginUser);
                }

            });


            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            var joinUser = users[userName];
            var sendMessage = new ApiSendMessage();
            _ = Task.Run(() => sendMessage.AddUserLobbyMember(joinUser));

            var test = JsonConvert.SerializeObject(response);
            Console.WriteLine(test);
            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class CreateRoomController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.CreateRoom request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.CreateRoom response = await CreateRoom(request.RoomName!, request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }

        public async Task<ApiResponse.CreateRoom> CreateRoom(string roomName, string userName)
        {
            var response = new ApiResponse.CreateRoom();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;

            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.CreateRoom Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }

            var user = serverManager.Users[userName];
            var rooms = serverManager.Rooms;
            //-- 유저 캐릭터 정보 가져오기


            int idx = -1;
            //-- 생성 가능한 방이 있는지 체크
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].CurrentMember == 0)
                {
                    idx = i;
                    Console.WriteLine($"생성 가능 방번호 {i}");

                    break;
                }
            }

            //-- 이미 최대 방 갯수 초과시
            if (idx == -1)
            {
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = "생성 가능한 방이 없습니다.";
                Console.WriteLine(response.Message);
                return response;
            }

            await Task.Run(() =>
            {
                //var roomUser = serverManager.GetRoomUserInfo(userName);

                var room = rooms[idx];
                //-- 방 셋팅
                room.RoomNumber = idx;
                room.CurrentMember = 1;
                room.RoomName = roomName;
                room.OwnerName = userName;

                //-- 유저 상태변경 셋팅
                user.State = (int)UserState.Room;
                user.RoomNumber = idx;
                user.SlotNumber = 0;

                response.State = user.State;
                response.RoomNumber = user.RoomNumber;
                response.SlotNumber = user.SlotNumber;
                response.CurrentMember = room.CurrentMember;
                response.RoomName = room.RoomName;
                response.OwnerName = response.OwnerName;

                response.Message = "Success";
                response.MessageCode = (int)MessageCode.Success;
            });
            var sendMessage = new ApiSendMessage();
            _ = Task.Run(() => sendMessage.AddChatRoomLobbyMember(user));

            int count = 0;
            foreach (var test in rooms)
            {
                if (test.Value.RoomName == null) continue;
                count++;
            }
            Console.WriteLine($"CreatedRoom:: 현재 생성된 방의 갯수: {count}");

            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class JoinRoomController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.JoinRoom request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.JoinRoom response = await JoinRoom(request.RoomNumber!, request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.JoinRoom> JoinRoom(int roomNumber, string userName)
        {
            var response = new ApiResponse.JoinRoom();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.JoinRoom Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }
            var user = serverManager.Users[userName];

            var room = serverManager.Rooms[roomNumber];
            //-- 인원 초과
            if (room.CurrentMember >= 6)
            {
                response.Message = "방이 가득차서 입장할 수 없습니다.";
                response.MessageCode = (int)MessageCode.Fail;
                return response;
            }

            await Task.Run(() =>
            {
                room.CurrentMember += 1;
                user.State = (int)UserState.Room;
                user.RoomNumber = room.RoomNumber;

                int slot = 0;
                //-- 채팅방 빈자리 슬롯 찾기
                foreach (var info in users)
                {
                    var userInfo = info.Value;
                    if (userInfo.State != (int)UserState.Room) continue;
                    if (userInfo.RoomNumber != user.RoomNumber) continue;
                    if (userInfo.SlotNumber == slot) slot++;
                }
                user.SlotNumber = slot;

                response.State = user.State;
                response.RoomNumber = user.RoomNumber;
                response.SlotNumber = user.SlotNumber;
                response.CurrentMember = room.CurrentMember;
                response.RoomName = room.RoomName;
                response.OwnerName = room.OwnerName;
                response.Characters = new List<Character>();
                foreach (var roomUserInfo in users)
                {
                    var roomUser = roomUserInfo.Value;
                    if (roomUser.State != (int)UserState.Room) continue;
                    if (roomUser.RoomNumber != user.RoomNumber) continue;

                    var character = serverManager.GetCharacter(roomUser.UserName!);

                    response.Characters.Add(character);
                }
            });
            var character = serverManager.GetCharacter(userName);
            var sendMessage = new ApiSendMessage();
            //-- 방에 입장한 유저들에게 메시지 송신
            _ = Task.Run(() => sendMessage.JoinRoomMember(room.CurrentMember, character));

            //-- 로비 유저들에게 유저 상태 송신
            _ = Task.Run(() => sendMessage.RoomLobbyMember(user.UserName!, user.State, user.RoomNumber, room.CurrentMember));
            response.Message = "Success";
            response.MessageCode = (int)MessageCode.Success;
            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class ExitRoomController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.Packet request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.ExitRoom response = await ExitRoom(request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.ExitRoom> ExitRoom(string userName, int opcode = 0)
        {
            var response = new ApiResponse.ExitRoom();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.ExitRoom Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }

            var user = users[userName];
            int state = user.State;

            var sendMessage = new ApiSendMessage();
            if (opcode == (int)Opcode.Logout)
            {
                int roomNumber = user.RoomNumber;
                int slotNumber = user.SlotNumber;

                if (state == (int)UserState.Room)
                {
                    var room = serverManager.Rooms[roomNumber];
                    room.CurrentMember--;
                    if (room.CurrentMember <= 0)
                    {
                        room.RoomName = "";
                        room.OwnerName = "";
                    }
                    _ = Task.Run(() => sendMessage.ExitRoomMember(roomNumber, slotNumber, user.UserName!, room.CurrentMember));
                }
                int logOut = (int)UserState.Logout;
                _ = Task.Run(() => sendMessage.LobbyMember(userName, logOut));

                users.TryRemove(userName, out _);
                response.Message = "Success";
                response.MessageCode = (int)MessageCode.Success;

                return response;
            }
            await Task.Run(() =>
            {

                switch (state)
                {
                    case (int)UserState.Room:
                        {
                            int roomNumber = user.RoomNumber;
                            int slotNumber = user.SlotNumber;
                            var room = serverManager.Rooms[roomNumber];
                            room.CurrentMember--;
                            if (room.CurrentMember <= 0)
                            {
                                room.RoomName = "";
                                room.OwnerName = "";
                            }
                            user.State = (int)UserState.Lobby;
                            user.RoomNumber = -1;
                            user.SlotNumber = -1;

                            _ = Task.Run(() => sendMessage.RoomLobbyMember(userName, user.State, roomNumber, room.CurrentMember));
                            _ = Task.Run(() => sendMessage.ExitRoomMember(roomNumber, slotNumber, user.UserName!, room.CurrentMember));
                        }
                        break;
                    default:
                        {
                            //-- 유저 상태 로비로 변경
                            user.State = (int)UserState.Lobby;
                            user.RoomNumber = -1;
                            user.SlotNumber = -1;
                            _ = Task.Run(() => sendMessage.LobbyMember(user.UserName!, user.State));
                        }
                        break;

                }
            });

            var rooms = serverManager.Rooms;
            response.Rooms = new Dictionary<int, Room>();
            foreach (var roomInfo in rooms)
            {
                var room = roomInfo.Value;
                if (room.CurrentMember <= 0) continue;
                response.Rooms![room.RoomNumber] = room;
            }

            response.LoginUsers = new List<LoginUser>();
            foreach (var loginUserInfo in users)
            {
                var info = loginUserInfo.Value;
                var loginUser = new LoginUser();
                loginUser!.State = info.State;
                loginUser.RoomNumber = info.RoomNumber;
                loginUser.UserName = info.UserName;
                response.LoginUsers!.Add(loginUser);
            }
            response.Message = "Success";
            response.MessageCode = (int)MessageCode.Success;
            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class SceneChangeController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.SceneChange request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine($"SceneChangeController:{testData}");

            ApiResponse.SceneChange response = await SceneChange(request.UserName!, request.State);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.SceneChange> SceneChange(string userName, int state)
        {
            var response = new ApiResponse.SceneChange();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.SceneChange Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }
            var user = users[userName];

            await Task.Run(() =>
            {
                user.State = state;
                var sendMessage = new ApiSendMessage();

                _ = Task.Run(() => sendMessage.LobbyMember(userName, user.State));
            });

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            response.State = user.State;

            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class BuyItemController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.BuyItem request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.BuyItem response = await BuyItem(request.ItemId!, request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.BuyItem> BuyItem(int itemId, string userName)
        {
            var response = new ApiResponse.BuyItem();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.BuyItem Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }
            var user = serverManager.Users[userName];
            var itemTable = Database.GetInstance().ItemTable;
            int price = itemTable[itemId].Price;
            //-- 이미 보유하고 있는 아이템의 경우
            if (user.Items!.ContainsKey(itemId))
            {
                response.Message = "이미 가지고 있습니다.";
                response.MessageCode = (int)MessageCode.Fail;
                return response;
            }

            if (user.Money >= price)
            {
                var paramList = new List<Dictionary<string, object?>>();
                var param = new Dictionary<string, object?>();
                string updateSql = $"UPDATE account SET money = money - {price} WHERE uuid = @uuid";
                param["@uuid"] = userName;
                paramList.Add(param);
                param.Clear();

                string insertSql = $"INSERT INTO inventory (uuid, item_id) VALUES (@uuid, @item_id)";
                param["@uuid"] = userName;
                param["@item_id"] = itemId;
                paramList.Add(param);

                // 여러 개의 쿼리를 리스트로 묶어서 트랜잭션에 사용
                var sqlQueries = new List<string> { updateSql, insertSql };
                // 트랜잭션 사용
                int messageCode = await Database.GetInstance().ExecuteQueryWithTransaction(sqlQueries, paramList, (int)DB.UserDB);

                if (messageCode != (int)MessageCode.Success)
                {
                    response.Message = "서버에서 에러가 발생하였습니다";
                    response.MessageCode = (int)MessageCode.Fail;
                    response.Money = user.Money;
                    return response;
                }

                user.Money -= price;

                response.Message = "구매가 완료되었습니다.";
                response.MessageCode = (int)MessageCode.Success;
                response.ItemId = itemId;
                response.Money = user.Money;
                user.Items[itemId] = false;
            }
            else
            {
                response.Message = "잔액이 부족합니다.";
                response.MessageCode = (int)MessageCode.Fail;
            }

            return response;
        }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class EquipItemsController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiRequest.EquipItems request)
        {
            string? testData = JsonConvert.SerializeObject(request);
            Console.WriteLine(testData);

            ApiResponse.EquipItems response = await EquipItems(request.Items!, request.UserName!);
            string? jsonData = JsonConvert.SerializeObject(response);
            return Ok(jsonData);
        }
        public async Task<ApiResponse.EquipItems> EquipItems(Dictionary<int, bool> equipItems, string userName)
        {
            var response = new ApiResponse.EquipItems();
            var users = ServerManager.GetInstance().Users;
            if (!users.ContainsKey(userName))
            {
                var message = $"WebReadMessages.EquipItems Error!! {userName} KeyNotFound";
                response.MessageCode = (int)MessageCode.Fail;
                response.Message = message;
                Console.WriteLine(message);
                return response;
            }
            response!.Items = new Dictionary<int, bool>();

            var user = ServerManager.GetInstance().Users[userName];
            var items = user.Items;

            //-- 착용 변경요청한 아이템이 유저 인벤토리에 있는지 검증
            foreach (var changeItem in equipItems)
            {
                if (!items!.ContainsKey(changeItem.Key))
                {
                    response.Message = "잘못된 요청입니다.";
                    response.MessageCode = (int)MessageCode.BadRequest;
                    foreach (var item in items)
                    {
                        response.Items[item.Key] = item.Value;
                    }
                    Console.WriteLine($"Warning!! [{userName}]: 인벤토리에 없는 아이템 착용 변경 요청!!");
                    return response;
                }
            }

            //-- 유저 인벤토리 DB 업데이트
            var sqlList = new List<string>();
            var param = new Dictionary<string, object?>();
            var paramList = new List<Dictionary<string, object?>>();

            foreach (var item in equipItems)
            {
                string sql = $"UPDATE inventory SET is_equip = {item.Value} WHERE uuid = @uuid AND item_id = {item.Key}";
                param["@uuid"] = userName;
                paramList.Add(param);
                sqlList.Add(sql);
            }
            int messageCode = await Database.GetInstance().ExecuteQueryWithTransaction(sqlList, paramList, (int)DB.UserDB);

            if (messageCode == (int)MessageCode.Success)
            {
                foreach (var item in equipItems)
                {
                    user.Items![item.Key] = item.Value;
                }
                //-- response 데이터 삽입
                response.Message = "의상이 변경되었습니다.";
                response.MessageCode = (int)MessageCode.Success;
            }
            else
            {
                //-- response 데이터 삽입
                response.Message = "서버에서 에러가 발생하였습니다.";
                response.MessageCode = (int)MessageCode.Fail;
            }


            var userItems = user.Items;
            foreach (var item in userItems!)
            {
                response.Items[item.Key] = item.Value;
            }

            return response;
        }

    }




    public class ApiSendMessage
    {
        public async Task RoomLobbyMember(string userName, int state, int roomNumber, int currentMember)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ServerPacket.RoomLobbyMember();

                packet!.UserName = userName;
                packet.State = state;
                packet.RoomNumber = roomNumber;
                packet.CurrentMember = currentMember;

                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.RoomLobbyMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }
        public async Task AddChatRoomLobbyMember(User user)
        {
            await Task.Run(() =>
            {
                var packet = new ServerPacket.AddChatRoomLobbyMember();
                packet!.State = user.State;
                packet.UserName = user.UserName;
                packet.RoomNumber = user.RoomNumber;

                var room = ServerManager.GetInstance().Rooms[user.RoomNumber];
                packet.CurrentMember = room.CurrentMember;
                packet.RoomName = room.RoomName;

                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.AddChatRoomLobbyMember;
                message.Message = JsonConvert.SerializeObject(packet);

                var messages = ServerManager.GetInstance().ChatMessages;
                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }
        public async Task AddUserLobbyMember(User user)
        {
            await Task.Run(() =>
            {
                var packet = new ServerPacket.AddUserLobbyMember();
                packet!.State = user.State;
                packet.RoomNumber = user.RoomNumber;
                packet.UserName = user.UserName;

                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.AddUserLobbyMember;
                message.Message = JsonConvert.SerializeObject(packet);

                var messages = ServerManager.GetInstance().ChatMessages;
                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }
        //-- 로비 유저들에게 변경된 유저 상태 송신
        public async Task LobbyMember(string userName, int state)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ServerPacket.LobbyMember();

                packet!.UserName = userName;
                packet.State = state;

                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.LobbyMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }

        public async Task JoinRoomMember(int currentMember, Character character)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ServerPacket.JoinRoomMember();
                packet!.CurrentMember = currentMember;
                packet.SlotNumber = character.SlotNumber;
                packet.UserName = character.UserName;
                packet.Gender = character.Gender;
                packet.Model = character.Model;
                packet.EquipItems = character.Items;

                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.JoinRoomMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }
        public async Task ExitRoomMember(int roomNumber, int slotNumber, string userName, int currentMember)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ServerPacket.ExitRoomMember();
                packet!.RoomNumber = roomNumber;
                packet.SlotNumber = slotNumber;
                packet.UserName = userName;
                packet.CurrentMember = currentMember;
                var message = new ServerPacket.Packet();
                message!.Opcode = (int)Opcode.ExitRoomMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }



    }



}