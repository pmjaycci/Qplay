using System.Collections.Concurrent;
using Newtonsoft.Json;
using Util;
using GameInfo;
using ZstdNet;

namespace QplayChatServer.server
{
    public class WebReadMessages
    {
        #region Singleton
        private static WebReadMessages? instance;
        private WebReadMessages() { }
        public static WebReadMessages GetInstance()
        {
            if (instance == null)
            {
                instance = new WebReadMessages();
            }
            return instance;
        }
        #endregion

        /* 게임 입장하기 구조  
        * Tcp서버에도 JoinGame Opcode로 추가 전송후 서버에서 해당 TCP클라이언트 Dictionary에 캐싱하여 관리
        유저 인벤토리 테이블 오픈후
        ServerManager.Users에 인벤토리, 캐릭터 정보, 상태 저장
        ==
        - [해당 유저 닉네임, 상태(로비), 방 번호] 로비 유저 클라이언트에 전송 1
        */
        public async Task<ApiResponse.JoinGame> JoinGame(string userName)
        {
            var response = new ApiResponse.JoinGame();
            var serverManager = ServerManager.GetInstance();
            var users = serverManager.Users;
            if (!users.ContainsKey(userName))
            {
                User user = new User();
                user!.UserName = userName;
                user.State = (int)UserState.Lobby;
                user.RoomNumber = -1;
                user.SlotNumber = -1;
                user.Items = new ConcurrentDictionary<int, bool>();

                response.State = (int)UserState.Lobby;
                response.RoomNumber = user.RoomNumber;
                response.SlotNumber = user.SlotNumber;
                response.UserName = user!.UserName;
                response.Items = new Dictionary<int, bool>();

                var sql = $"SELECT gender, model, money, inventory.item_id, inventory.is_equip FROM account LEFT JOIN inventory ON account.uuid = inventory.uuid WHERE account.uuid = @uuid";
                var param = new Dictionary<string, object?>();
                param["@uuid"] = userName;

                int itemId;
                bool isEquip;

                using (var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB))
                {
                    while (result.Read())
                    {
                        user.Gender = Convert.ToInt32(result["gender"]);
                        user.Money = Convert.ToInt32(result["money"]);
                        user.Model = Convert.ToInt32(result["model"]);
                        response.Gender = user.Gender;
                        response.Model = user.Model;
                        response.Money = user.Money;
                        bool isItemNull = result.IsDBNull(result.GetOrdinal("item_id"));
                        if (!isItemNull)
                        {
                            itemId = Convert.ToInt32(result["item_id"]);
                            isEquip = Convert.ToBoolean(result["is_equip"]);
                            user.Items[itemId] = isEquip;
                            response.Items![itemId] = isEquip;
                        }
                    }

                }
                if (users.TryAdd(userName, user))
                {
                    Console.WriteLine($"유저 생성함 : {userName}");
                }
                else
                {
                    Console.WriteLine($"유저 생성 실패 : {userName}");
                }


            }
            else
            {
                var userInfo = users[userName];
                response!.State = (int)UserState.Lobby;
                response.RoomNumber = -1;
                response.SlotNumber = -1;
                response.UserName = userInfo.UserName;
                response.Gender = userInfo.Gender;
                response.Model = userInfo.Model;
                response.Money = userInfo.Money;
                response.Items = new Dictionary<int, bool>();
                foreach (var item in userInfo.Items!)
                {
                    response.Items[item.Key] = item.Value;
                }
            }

            response!.Rooms = new Dictionary<int, Room>();
            var rooms = serverManager.Rooms;
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


            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            var joinUser = users[userName];

            _ = Task.Run(() => AddUserLobbyMember(joinUser));

            var test = JsonConvert.SerializeObject(response);
            Console.WriteLine(test);
            return response;
        }
        //-- 로비 유저들에게 접속한 유저 정보 전송

        /* 방 생성하기 구조 
        미리 생성된 채팅방 목록중 RoomName 값이 null값인 채팅방 탐색후
        유저가 요청한 데이터로 채팅방 정보 갱신 및 
        ServerManager.Users의 상태, 방번호 변경
        ==
        - [해당 유저 닉네임, 상태(채팅방), 방 번호] 로비 유저 클라이언트에 전송 1
        - [해당 방 정보 (방 번호, 제목, 인원수, 방 인원 닉네임)] 로비 유저 클라이언트에 전송 2
        */
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

            _ = Task.Run(() => AddChatRoomLobbyMember(user));

            int count = 0;
            foreach (var test in rooms)
            {
                if (test.Value.RoomName == null) continue;
                count++;
            }
            Console.WriteLine($"CreatedRoom:: 현재 생성된 방의 갯수: {count}");

            return response;
        }
        //-- 로비 유저들에게 생성된 방 정보 송신


        /* 방 입장하기 구조
        ==
        - [해당 유저 닉네임, 상태(채팅방), 방 번호] 로비 유저 클라이언트에 전송 1
        - [해당 방 정보 (방 번호, 인원수, 방 인원 닉네임)] 로비 유저 클라이언트에 전송 3
        - [해당 유저 정보 (닉네임, 의상), 자리 슬롯 번호] 접속하는 채팅방 유저 클라이언트에 전송 4
        */
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
            //-- 방에 입장한 유저들에게 메시지 송신
            _ = Task.Run(() => JoinRoomMember(room.CurrentMember, character));

            //-- 로비 유저들에게 유저 상태 송신
            _ = Task.Run(() => RoomLobbyMember(user.UserName!, user.State, user.RoomNumber, room.CurrentMember));
            response.Message = "Success";
            response.MessageCode = (int)MessageCode.Success;
            return response;
        }

        //TODO: 로그아웃관련 처리해야함 (채팅방 내에서 로그아웃 또는 로비에서 로그아웃 )
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
                    _ = Task.Run(() => ExitRoomMember(roomNumber, slotNumber, user.UserName!, room.CurrentMember));
                }
                int logOut = (int)UserState.Logout;
                _ = Task.Run(() => LobbyMember(userName, logOut));

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

                            _ = Task.Run(() => RoomLobbyMember(userName, user.State, roomNumber, room.CurrentMember));
                            _ = Task.Run(() => ExitRoomMember(roomNumber, slotNumber, user.UserName!, room.CurrentMember));
                        }
                        break;
                    default:
                        {
                            //-- 유저 상태 로비로 변경
                            user.State = (int)UserState.Lobby;
                            user.RoomNumber = -1;
                            user.SlotNumber = -1;
                            _ = Task.Run(() => LobbyMember(user.UserName!, user.State));
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
                _ = Task.Run(() => LobbyMember(userName, user.State));
            });

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            response.State = user.State;

            return response;
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
            var shopTable = Database.GetInstance().ShopTable;
            int price = shopTable[itemId].Price;
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


        //-- 로비 유저들에게 방 입/퇴장 유저 및 갱신된 방 정보 (방번호, 인원수) 송신
        public async Task RoomLobbyMember(string userName, int state, int roomNumber, int currentMember)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ChatBase.RoomLobbyMember();

                packet!.UserName = userName;
                packet.State = state;
                packet.RoomNumber = roomNumber;
                packet.CurrentMember = currentMember;

                var message = new ChatBase.Packet();
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
                var packet = new ChatBase.AddChatRoomLobbyMember();
                packet!.State = user.State;
                packet.UserName = user.UserName;
                packet.RoomNumber = user.RoomNumber;

                var room = ServerManager.GetInstance().Rooms[user.RoomNumber];
                packet.CurrentMember = room.CurrentMember;
                packet.RoomName = room.RoomName;

                var message = new ChatBase.Packet();
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
                var packet = new ChatBase.AddUserLobbyMember();
                packet!.State = user.State;
                packet.RoomNumber = user.RoomNumber;
                packet.UserName = user.UserName;

                var message = new ChatBase.Packet();
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
                var packet = new ChatBase.LobbyMember();

                packet!.UserName = userName;
                packet.State = state;

                var message = new ChatBase.Packet();
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
                var packet = new ChatBase.JoinRoomMember();
                packet!.CurrentMember = currentMember;
                packet.SlotNumber = character.SlotNumber;
                packet.UserName = character.UserName;
                packet.Gender = character.Gender;
                packet.Model = character.Model;
                packet.EquipItems = character.Items;

                var message = new ChatBase.Packet();
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
                var packet = new ChatBase.ExitRoomMember();
                packet!.RoomNumber = roomNumber;
                packet.SlotNumber = slotNumber;
                packet.UserName = userName;
                packet.CurrentMember = currentMember;
                var message = new ChatBase.Packet();
                message!.Opcode = (int)Opcode.ExitRoomMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }



    }



}