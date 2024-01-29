using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Packet;
using Util;

namespace QplayChatServer.server
{
    public class ServerManager
    {
        private static ServerManager? instance;
        private ServerManager()
        {
            Room room = new Room();
            room!.RoomName = null;
            room!.Users = new ConcurrentBag<RoomUser>();
            RoomUser roomUser = new RoomUser();
            roomUser!.Name = null;
            for (int i = 0; i < 6; i++)
            {
                room.Users!.Add(roomUser);
            }
            for (int i = 0; i < 100; i++)
            {
                RoomList.Add(room);
            }
        }

        public static ServerManager GetInstance()
        {
            if (instance == null)
            {
                instance = new ServerManager();
            }
            return instance;
        }

        public CancellationToken Token;

        //-- 유저 정보 캐싱 : 상태, 방번호, 이름, 착용아이템 등..
        public ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();

        #region Http Server
        //-- 채팅방 유저정보 가져오기
        public RoomUser GetRoomUser(string userName)
        {
            var roomUser = new RoomUser();
            var user = Users[userName];
            roomUser.Name = userName;
            roomUser.Gender = user.Gender;
            roomUser.Model = user.Model;
            roomUser.EquipItems = GetEquipItemList(userName);
            return roomUser;
        }
        //-- 착용한 아이템 정보 가져오기
        public ConcurrentBag<int>? GetEquipItemList(string userName)
        {
            ConcurrentBag<int> itemList = new ConcurrentBag<int>();

            var userItemList = Users[userName].Items;

            if (userItemList!.Count <= 0) return null;

            foreach (var item in userItemList!)
            {
                if (!item.Value) continue;
                itemList.Add(item.Key);
            }
            return itemList;
        }

        //-- 생성된 채팅방 정보 캐싱 : 방 제목, 방장 이름, 입장 유저 정보
        public ConcurrentBag<Room> RoomList = new ConcurrentBag<Room>();

        //-- 생성된 채팅방 목록 가져오기 : 방제목이 null이 아닌 채팅방만 반환
        public ConcurrentBag<Room> GetRoomList()
        {
            var roomList = new ConcurrentBag<Room>();
            foreach (var room in RoomList)
            {
                if (room.RoomName == null) continue;
                roomList.Add(room);
            }

            return roomList;
        }
        #endregion

        #region Tcp Server
        //-- 연결된 클라이언트 정보 캐싱 : 유저 명, 클라이언트
        public ConcurrentDictionary<string, TcpClient> Clients = new ConcurrentDictionary<string, TcpClient>();

        //-- 클라이언트들에게 메시지 전달 : ConcurrentQueue에 담긴 클라이언트들에게 메시지 전달
        public async Task<BasePacket> BroadcastMessage(ConcurrentQueue<BroadcastUser> users, int opcode, string message)
        {
            var packet = new BasePacket();
            packet!.Opcode = opcode;
            packet!.Message = message;
            var broadcastPacket = JsonConvert.SerializeObject(packet);

            Console.WriteLine($"TCP 받은 메시지: {message}");
            foreach (var user in users)
            {
                //-- 이미 연결 종료된 클라이언트일경우 건너뛰기
                if (!Clients.ContainsKey(user.UserName!)) continue;

                using (NetworkStream stream = user.Client!.GetStream())
                {
                    byte[] broadcastBuffer = Encoding.UTF8.GetBytes(broadcastPacket);
                    await stream.WriteAsync(broadcastBuffer, 0, broadcastBuffer.Length, Token);
                }
            }

            return packet;
        }

        //-- 상태에 따른 전달 받을 클라이언트들 가져오기
        public async Task<ConcurrentQueue<BroadcastUser>> GetBroadcastUsers(string requestUserName, int state)
        {
            ConcurrentQueue<BroadcastUser> result = new ConcurrentQueue<BroadcastUser>();
            BroadcastUser broadcastUser = new BroadcastUser();
            await Task.Run(() =>
            {
                foreach (var user in Users)
                {
                    if (user.Key == requestUserName) continue;
                    if (!Clients.ContainsKey(user.Key)) continue;

                    //-- 호출한 유저의 상태와 동일한 상태를 가진 유저인지?
                    if (user.Value.State == state)
                    {
                        //-- 채팅방에 있는 상태일 경우
                        if (user.Value.State == (int)UserState.Room)
                        {
                            int roomNumber = Users[requestUserName].RoomNumber;
                            //-- 호출한 유저와 동일한 방번호를 가진 유저일경우
                            if (user.Value.RoomNumber == roomNumber)
                            {
                                broadcastUser.UserName = user.Key;
                                broadcastUser.Client = Clients[user.Key];
                                result.Enqueue(broadcastUser);
                            }
                        }
                        //-- 로비에 있는 상태일 경우
                        else
                        {
                            broadcastUser.UserName = user.Key;
                            broadcastUser.Client = Clients[user.Key];
                            result.Enqueue(broadcastUser);
                        }
                    }
                }
            });

            return result;
        }
        #endregion
    }
}

public class BroadcastUser
{
    public TcpClient? Client;
    public string? UserName;
}

public class BroadcastUserMessage : BroadcastUser
{
    public BasePacket? Message;
}
