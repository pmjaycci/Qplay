using System.Collections.Concurrent;
using GameInfo;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.IO;
using Util;

namespace QplayChatServer.server
{
    public class WebReadMessages
    {
        #region Singleton
        private static WebReadMessages? instance;
        //private WebReadMessages() { }
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
            response!.UserName = userName;
            response.State = (int)UserState.Lobby;
            response.RoomNumber = 0;
            response.Items = new Dictionary<int, bool>();

            UserInfo userInfo = new UserInfo();
            userInfo!.UserName = userName;
            userInfo.State = (int)UserState.Lobby;
            userInfo.RoomNumber = 0;
            userInfo.Items = new ConcurrentDictionary<int, bool>();

            int itemId;
            bool isEquip;

            var sql = $"SELECT gender, model, money, inventory.item_id, inventory.is_equip FROM account LEFT JOIN inventory ON account.uuid = inventory.uuid WHERE account.uuid = @uuid";
            var param = new Dictionary<string, object?>();
            param["@uuid"] = userName;

            using (var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB))
            {
                while (result.Read())
                {
                    userInfo.Gender = Convert.ToInt32(result["gender"]);
                    userInfo.Money = Convert.ToInt32(result["money"]);
                    userInfo.Model = Convert.ToInt32(result["model"]);
                    response.Gender = userInfo.Gender;
                    response.Model = userInfo.Model;
                    response.Money = userInfo.Money;
                    bool isItemNull = result.IsDBNull(result.GetOrdinal("item_id"));
                    if (!isItemNull)
                    {
                        itemId = Convert.ToInt32(result["item_id"]);
                        isEquip = Convert.ToBoolean(result["is_equip"]);
                        userInfo.Items[itemId] = isEquip;
                        response.Items![itemId] = isEquip;
                    }
                }

            }


            //-- 게임 접속 유저 리스트 받기 (response에 데이터 삽입)
            var usersInfo = ServerManager.GetInstance().Users;
            usersInfo[userName] = userInfo;
            var user = usersInfo[userName];
            var roomsInfo = ServerManager.GetInstance().JoinRooms;


            response!.CreatedRoomsInfo = new Dictionary<int, CreatedRoomInfo>();
            response.LobbyUsersInfo = new List<LobbyUserInfo>();

            foreach (var info in usersInfo)
            {
                var lobbyUserInfo = new LobbyUserInfo();
                lobbyUserInfo!.UserName = info.Value.UserName;
                lobbyUserInfo.State = info.Value.State;
                lobbyUserInfo.RoomNumber = info.Value.RoomNumber;

                response.LobbyUsersInfo.Add(lobbyUserInfo);
            }
            //-- 생성된 방 정보 받기 (response에 데이터 삽입)
            foreach (var roomInfo in roomsInfo)
            {
                if (roomInfo.Value.RoomName == null) continue;

                int createdRoomNumber = roomInfo.Key;
                var createdRoomInfo = new CreatedRoomInfo();
                createdRoomInfo!.RoomNumber = createdRoomNumber;
                createdRoomInfo.CurrentMember = roomInfo.Value.CurrentMember;
                createdRoomInfo.RoomName = roomInfo.Value.RoomName;
                createdRoomInfo.OwnerName = roomInfo.Value.OwnerName;
                createdRoomInfo.RoomUsersInfo = new List<string>();
                foreach (var roomUserInfo in roomInfo.Value.JoinRoomUsersInfo!)
                {
                    var roomUserName = roomUserInfo.Value.UserName;
                    createdRoomInfo.RoomUsersInfo.Add(roomUserName!);
                }
                response.CreatedRoomsInfo[roomInfo.Key] = createdRoomInfo;
            }

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";

            await AddUserLobbyMember(user);
            return response;
        }

        /* 방 생성하기 구조 
        미리 생성된 채팅방 목록중 RoomName 값이 null값인 채팅방 탐색후
        유저가 요청한 데이터로 채팅방 정보 갱신 및 
        ServerManager.Users의 상태, 방번호 변경
        ==
        - [해당 유저 닉네임, 상태(채팅방), 방 번호] 로비 유저 클라이언트에 전송 1
        - [해당 방 정보 (방 번호, 제목, 인원수, 방 인원 닉네임)] 로비 유저 클라이언트에 전송 2
        */
        public async Task<ApiResponse.Room> CreateRoom(string roomName, string userName)
        {
            var response = new ApiResponse.Room();
            var serverManager = ServerManager.GetInstance();
            var user = serverManager.Users[userName];
            var rooms = serverManager.JoinRooms;
            await Task.Run(() =>
            {
                //-- 유저정보 가져오기
                var roomUser = serverManager.GetRoomUserInfo(userName);
                int idx = -1;
                //-- 생성 가능한 방이 있는지 체크
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].RoomName == null)
                    {
                        idx = i;
                        break;
                    }
                }

                //-- 이미 최대 방 갯수 초과시
                if (idx == -1)
                {
                    response.Message = "생성 가능한 방이 없습니다.";
                    response.MessageCode = (int)MessageCode.Fail;
                }
                //-- 방 생성 가능시
                else
                {
                    //-- 방 셋팅
                    rooms[idx].RoomName = roomName;
                    Console.WriteLine($"CreateRoom:: 생성된 방 제목 {rooms[idx].RoomName} / 유저명:{userName}");
                    rooms[idx].OwnerName = userName;
                    rooms[idx].CurrentMember = 1;

                    response.RoomName = roomName;
                    response.OwnerName = userName;
                    response.CurrentMember = 1;

                    //-- 자리 슬롯 셋팅
                    response.JoinRoomUsersInfo = new Dictionary<int, JoinRoomUserInfo>();
                    rooms[idx].JoinRoomUsersInfo![0] = roomUser;
                    response.JoinRoomUsersInfo[0] = roomUser;

                    //-- 유저 상태변경 셋팅
                    user.State = (int)UserState.Room;
                    user.RoomNumber = idx;


                    response.State = (int)UserState.Room;
                    response.RoomNumber = idx;
                    response.Message = "Success";
                    response.MessageCode = (int)MessageCode.Success;
                }

            });
            await AddChatRoomLobbyMember(user);

            int count = 0;
            foreach (var test in rooms)
            {
                if (test.Value.RoomName == null) continue;
                count++;
            }
            Console.WriteLine($"CreateRoom:: 현재 생성된 방의 갯수: {count}");

            return response;
        }

        /* 방 입장하기 구조
        ==
        - [해당 유저 닉네임, 상태(채팅방), 방 번호] 로비 유저 클라이언트에 전송 1
        - [해당 방 정보 (방 번호, 인원수, 방 인원 닉네임)] 로비 유저 클라이언트에 전송 3
        - [해당 유저 정보 (닉네임, 의상), 자리 슬롯 번호] 접속하는 채팅방 유저 클라이언트에 전송 4
        */
        public async Task<ApiResponse.Room> JoinRoom(int roomNumber, string userName)
        {
            var response = new ApiResponse.Room();
            int slotNumber = 0;

            await Task.Run(async () =>
            {
                var serverManager = ServerManager.GetInstance();
                var roomInfo = serverManager.JoinRooms[roomNumber];

                //-- 인원 초과
                if (roomInfo.CurrentMember == 6)
                {
                    response.Message = "방이 가득차서 입장할 수 없습니다.";
                    response.MessageCode = (int)MessageCode.Fail;
                }
                else
                {
                    serverManager.JoinRooms[roomNumber].CurrentMember++;
                    //-- 방정보에 삽입할 유저정보 가져오기
                    var roomUserInfo = serverManager.GetRoomUserInfo(userName);
                    var user = serverManager.Users[userName];

                    //-- 입장하려는 방에 빈자리 탐색후 해당유저 정보 설정
                    foreach (var userInfo in roomInfo.JoinRoomUsersInfo!)
                    {
                        var userName = userInfo.Value.UserName;
                        if (userName != null) continue;
                        slotNumber = userInfo.Key;
                        //-- 해당 방에 유저정보 셋팅
                        roomInfo.JoinRoomUsersInfo![slotNumber] = roomUserInfo;
                        user.State = (int)UserState.Room;
                        user.RoomNumber = roomNumber;


                        //-- 해당 방정보 response에 데이터 삽입
                        response.Message = "Success";
                        response.MessageCode = (int)MessageCode.Success;

                        response.State = (int)UserState.Room;
                        response.CurrentMember = roomInfo.CurrentMember;
                        response.RoomName = roomInfo.RoomName;
                        response.OwnerName = roomInfo.OwnerName;
                        response.JoinRoomUsersInfo = new Dictionary<int, JoinRoomUserInfo>();
                        var roomUsersInfo = roomInfo.JoinRoomUsersInfo;
                        foreach (var info in roomUsersInfo!)
                        {
                            var nullUser = info.Value;
                            if (nullUser.UserName == null) continue;
                            response.JoinRoomUsersInfo[info.Key] = info.Value;
                        }

                        break;
                    }
                    //-- 방에 입장한 유저들에게 메시지 송신
                    await JoinRoomMember(user, roomInfo, roomNumber, slotNumber);

                    //-- 로비 유저들에게 유저 상태 송신
                    await RoomLobbyMember(userName, (int)UserState.Room, roomNumber, roomInfo.CurrentMember);

                }
            });

            return response;
        }
        public async Task<ApiResponse.ExitRoom> ExitRoom(string userName)
        {
            var response = new ApiResponse.ExitRoom();
            response!.CreatedRoomsInfo = new Dictionary<int, CreatedRoomInfo>();
            response.LobbyUsersInfo = new List<LobbyUserInfo>();
            await Task.Run(async () =>
            {
                var user = ServerManager.GetInstance().Users[userName];
                int state = user.State;
                //-- 현재 유저의 위치에 따른 처리
                switch (state)
                {
                    case (int)UserState.Room:
                        {
                            int roomNumber = ServerManager.GetInstance().Users[userName].RoomNumber;
                            var room = ServerManager.GetInstance().JoinRooms[roomNumber];
                            int slotNumber = 0;
                            //-- 퇴장하는 방에서 해당 유저정보 제거
                            foreach (var info in room.JoinRoomUsersInfo!)
                            {
                                if (info.Value.UserName != userName) continue;
                                slotNumber = info.Key;
                                info.Value.UserName = null;
                            }

                            //-- 현재 방인원 체크후 0명이 될경우 방제거
                            if (room.CurrentMember > 1)
                            {
                                room.CurrentMember--;
                            }
                            else
                            {
                                room.RoomName = null;
                            }
                            //-- 유저 상태 로비로 변경
                            user.State = (int)UserState.Lobby;
                            await RoomLobbyMember(userName, user.State, roomNumber, room.CurrentMember);
                            await ExitRoomMember(roomNumber, slotNumber);
                        }

                        break;
                    case (int)UserState.Logout:
                        {
                            int roomNumber = ServerManager.GetInstance().Users[userName].RoomNumber;
                            var room = ServerManager.GetInstance().JoinRooms[roomNumber];
                            int slotNumber = 0;
                            //-- 퇴장하는 방에서 해당 유저정보 제거
                            foreach (var info in room.JoinRoomUsersInfo!)
                            {
                                if (info.Value.UserName != userName) continue;
                                slotNumber = info.Key;
                                info.Value.UserName = null;
                            }

                            //-- 현재 방인원 체크후 0명이 될경우 방제거
                            if (room.CurrentMember > 1)
                            {
                                room.CurrentMember--;
                            }
                            else
                            {
                                room.RoomName = null;
                            }
                            //-- 유저 상태 로비로 변경
                            user.State = (int)UserState.Lobby;
                            await RoomLobbyMember(userName, user.State, roomNumber, room.CurrentMember);
                            await ExitRoomMember(roomNumber, slotNumber);
                        }
                        break;

                    default:
                        //-- 유저 상태 로비로 변경
                        user.State = (int)UserState.Lobby;
                        await LobbyMember(userName, user.State);
                        break;
                }


                //-- 게임 접속 유저 리스트 받기 (response에 데이터 삽입)
                var usersInfo = ServerManager.GetInstance().Users;
                var roomsInfo = ServerManager.GetInstance().JoinRooms;
                foreach (var userInfo in usersInfo)
                {
                    var lobbyUserInfo = new LobbyUserInfo();
                    lobbyUserInfo!.UserName = userInfo.Value.UserName;
                    lobbyUserInfo.State = userInfo.Value.State;
                    lobbyUserInfo.RoomNumber = userInfo.Value.RoomNumber;

                    response.LobbyUsersInfo.Add(lobbyUserInfo);
                }
                //-- 생성된 방 정보 받기 (response에 데이터 삽입)
                foreach (var roomInfo in roomsInfo)
                {
                    if (roomInfo.Value.RoomName == null) continue;
                    int createdRoomNumber = roomInfo.Key;
                    var createdRoomInfo = new CreatedRoomInfo();
                    createdRoomInfo!.RoomNumber = createdRoomNumber;
                    createdRoomInfo.CurrentMember = roomInfo.Value.CurrentMember;
                    createdRoomInfo.RoomName = roomInfo.Value.RoomName;
                    createdRoomInfo.OwnerName = roomInfo.Value.OwnerName;
                    createdRoomInfo.RoomUsersInfo = new List<string>();
                    foreach (var roomUserInfo in roomInfo.Value.JoinRoomUsersInfo!)
                    {
                        var roomUserName = roomUserInfo.Value.UserName;
                        createdRoomInfo.RoomUsersInfo.Add(roomUserName!);
                    }
                    response.CreatedRoomsInfo[roomInfo.Key] = createdRoomInfo;
                }
            });

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            response.State = (int)UserState.Lobby;


            return response;
        }

        public async Task<ApiResponse.SceneChange> SceneChange(string userName, int state)
        {
            var response = new ApiResponse.SceneChange();
            await Task.Run(async () =>
            {
                var user = ServerManager.GetInstance().Users[userName];
                //-- 유저 상태  변경
                user.State = state;
                await LobbyMember(userName, user.State);
            });

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            response.State = state;

            return response;
        }

        public async Task<ApiResponse.BuyItem> BuyItem(int itemId, string userName)
        {
            var response = new ApiResponse.BuyItem();
            var user = ServerManager.GetInstance().Users[userName];
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

        //-- 로비 유저들에게 접속한 유저 정보 전송
        public async Task AddUserLobbyMember(UserInfo user)
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

        //-- 로비 유저들에게 생성된 방 정보 송신
        public async Task AddChatRoomLobbyMember(UserInfo user)
        {
            await Task.Run(() =>
            {
                var packet = new ChatBase.AddChatRoomLobbyMember();
                packet!.State = user.State;
                packet.UserName = user.UserName;
                packet.RoomNumber = user.RoomNumber;

                var room = ServerManager.GetInstance().JoinRooms[user.RoomNumber];
                packet.CurrentMember = room.CurrentMember;
                packet.RoomName = room.RoomName;
                packet.OwnerName = room.OwnerName;

                var message = new ChatBase.Packet();
                message!.Opcode = (int)Opcode.AddChatRoomLobbyMember;
                message.Message = JsonConvert.SerializeObject(packet);

                var messages = ServerManager.GetInstance().ChatMessages;
                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
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

        public async Task JoinRoomMember(UserInfo user, JoinRoomInfo room, int roomNumber, int slotNumber)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ChatBase.JoinRoomMember();
                packet!.RoomNumber = roomNumber;
                packet.CurrentMember = room.CurrentMember;
                packet.SlotNumber = slotNumber;
                packet.UserName = user.UserName;
                packet.Gender = user.Gender;
                packet.Model = user.Model;
                packet.EquipItems = new List<int>();

                foreach (var item in user.Items!)
                {
                    var isEquip = item.Value;
                    if (!isEquip) continue;

                    packet.EquipItems.Add(item.Key);
                }

                var message = new ChatBase.Packet();
                message!.Opcode = (int)Opcode.JoinRoomMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }
        public async Task ExitRoomMember(int roomNumber, int slotNumber)
        {
            var messages = ServerManager.GetInstance().ChatMessages;
            await Task.Run(() =>
            {
                var packet = new ChatBase.ExitRoomMember();
                packet!.RoomNumber = roomNumber;
                packet.SlotNumber = slotNumber;

                var message = new ChatBase.Packet();
                message!.Opcode = (int)Opcode.ExitRoomMember;
                message.Message = JsonConvert.SerializeObject(packet);

                messages!.Enqueue(message);
                ServerManager.GetInstance().ChatSemaphore.Release();
            });
        }

    }



}