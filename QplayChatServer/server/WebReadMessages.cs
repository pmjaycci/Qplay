using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Org.BouncyCastle.Utilities;
using Packet;
using Util;

namespace QplayChatServer.server
{
    public class WebReadMessages
    {
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
        public async Task<ResponseJoinGame> InsertUserData(string name)
        {
            ResponseJoinGame response = new ResponseJoinGame();
            User user = new User();

            user!.Name = name;
            user.State = (int)UserState.Lobby;
            user.RoomNumber = 0;
            user.Items = new Dictionary<int, bool>();
            int itemId;
            bool isEquip;


            var sql = $"SELECT gender, model, money, inventory.item_id, inventory.is_equip FROM account LEFT JOIN inventory ON account.uuid = inventory.uuid WHERE account.uuid = @uuid";
            var param = new Dictionary<string, object?>();
            param["@uuid"] = name;

            var result = await Database.GetInstance().Query(sql, param, (int)DB.UserDB);

            while (result.Read())
            {
                user.Gender = Convert.ToInt32(result["gender"]);
                user.Model = Convert.ToInt32(result["model"]);
                user.Money = Convert.ToInt32(result["money"]);
                bool isItemNull = result.IsDBNull(result.GetOrdinal("item_id"));
                if (!isItemNull)
                {
                    itemId = Convert.ToInt32(result["item_id"]);
                    isEquip = Convert.ToBoolean(result["is_equip"]);
                    user.Items[itemId] = isEquip;
                }

            }

            ServerManager.GetInstance().Users[name] = user;

            response.MessageCode = (int)MessageCode.Success;
            response.Message = "Success";
            response.Data = user;
            return response;
        }

        public async Task<ResponseRoom> CreateRoom(string roomName, string userName)
        {
            var response = new ResponseRoom();
            await Task.Run(() =>
            {
                var user = ServerManager.GetInstance().Users[userName];

                var roomList = ServerManager.GetInstance().RoomList;
                var roomUser = ServerManager.GetInstance().GetRoomUser(userName);//new RoomUser();

                for (int i = 0; i < roomList.Count; i++)
                {
                    if (roomList[i].RoomName == null)
                    {
                        roomList[i].RoomName = roomName;
                        roomList[i].LeaderName = userName;
                        roomList[i].Users!.Add(roomUser);
                        user.State = (int)UserState.Room;
                        user.RoomNumber = i;
                        response.Message = "Success";
                        response.MessageCode = (int)MessageCode.Success;
                        response.Data = roomList[i];
                        return;
                    }
                }
                response.Message = "생성 가능한 방이 없습니다.";
                response.MessageCode = (int)MessageCode.Fail;
            });

            return response;
        }

        public async Task<ResponseRoom> JoinRoom(int roomNumber, string userName)
        {
            var response = new ResponseRoom();
            await Task.Run(() =>
            {
                var room = ServerManager.GetInstance().RoomList[roomNumber];
                var user = ServerManager.GetInstance().Users[userName];
                for (int i = 0; i < room.Users!.Count; i++)
                {
                    if (room.Users[i].Name == null)
                    {
                        room.Users[i].Name = userName;
                        room.Users[i].Gender = user.Gender;
                        room.Users[i].Model = user.Model;
                        room.Users[i].EquipItems = ServerManager.GetInstance().GetEquipItemList(userName);

                        user.State = (int)UserState.Room;
                        user.RoomNumber = roomNumber;

                        response.Message = "Success";
                        response.MessageCode = (int)MessageCode.Success;
                        response.Data = room;

                        return;
                    }
                }

                response.Message = "방이 가득차서 입장할 수 없습니다.";
                response.MessageCode = (int)MessageCode.Fail;
            });


            return response;
        }

        public async Task<ResponsePacket> ExitRoom(string userName)
        {
            var response = new ResponsePacket();
            await Task.Run(() =>
            {
                var user = ServerManager.GetInstance().Users[userName];
                var room = ServerManager.GetInstance().RoomList[user.RoomNumber];
                for (int i = 0; i < room.Users!.Count; i++)
                {
                    if (room.Users[i].Name == userName)
                    {
                        room.Users[i].Name = null;
                        room.Users[i].Gender = 0;
                        room.Users[i].Model = 0;
                        room.Users[i].EquipItems = null;

                        user.State = (int)UserState.Lobby;
                        user.RoomNumber = 0;

                        response.Message = "Success";
                        response.MessageCode = (int)MessageCode.Success;

                        return;
                    }
                }

                response.Message = "정상적인 방법이 아닙니다.";
                response.MessageCode = (int)MessageCode.BadRequest;
            });


            return response;
        }
        public async Task<ResponsePacket> Shop(int state, string userName)
        {
            var response = new ResponsePacket();
            await Task.Run(() =>
            {
                var user = ServerManager.GetInstance().Users[userName];
                user.State = state;

                response.Message = "Success";
                response.MessageCode = (int)MessageCode.Success;
            });


            return response;
        }

        public async Task<ResponseBuyItem> BuyItem(int itemId, string userName)
        {
            var response = new ResponseBuyItem();
            var user = ServerManager.GetInstance().Users[userName];
            var shopTable = Database.GetInstance().ShopTable;
            int price = shopTable[itemId].Price;

            if (user.Money >= price)
            {
                var paramList = new List<Dictionary<string, object?>>();
                var param = new Dictionary<string, object?>();
                string updateSql = $"UPDATE account SET money = money - {price} WHERE uuid = @uuid";
                param["@uuid"] = userName;
                paramList.Add(param);
                string insertSql = $"INSERT INTO account (uuid, item_id) VALUES (@uuid, {itemId})";
                param.Clear();
                param["@uuid"] = userName;
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
            }
            else
            {
                response.Message = "잔액이 부족합니다.";
                response.MessageCode = (int)MessageCode.Fail;
            }

            return response;
        }
        public async Task<ResponseChangeCharacter> ChangeCharacter(Dictionary<int, bool> equipItems, string userName)
        {
            var response = new ResponseChangeCharacter();
            var user = ServerManager.GetInstance().Users[userName];
            var items = user.Items;


            string? sql;
            var param = new Dictionary<string, object?>();
            foreach (var item in equipItems)
            {
                sql = $"UPDATE inventory SET item_id = {item.Key}, is_equip = {item.Value} WHERE uuid = @uuid";
                param["@uuid"] = userName;
                int messageCode = await Database.GetInstance().ExecuteQuery(sql, param, (int)DB.UserDB);
                if (messageCode == (int)MessageCode.Success)
                {
                    items![item.Key] = item.Value;
                }
            }

            response.Message = "의상이 변경되었습니다.";
            response.MessageCode = (int)MessageCode.Success;
            response.Items = items;
            return response;
        }
    }
}