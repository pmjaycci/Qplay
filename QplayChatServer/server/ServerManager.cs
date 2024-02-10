using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using GameInfo;
using Newtonsoft.Json;

namespace QplayChatServer.server
{
    public class ServerManager
    {
        #region Singleton
        private static ServerManager? instance;
        private ServerManager()
        {
            for (int roomNumber = 0; roomNumber < 100; roomNumber++)
            {
                var room = new JoinRoomInfo();
                room!.RoomName = null;
                room!.JoinRoomUsersInfo = new ConcurrentDictionary<int, JoinRoomUserInfo>();
                var roomUser = new JoinRoomUserInfo();
                roomUser!.UserName = null;
                for (int slotNumber = 0; slotNumber < 6; slotNumber++)
                {
                    room.JoinRoomUsersInfo[slotNumber] = roomUser;
                }
                JoinRooms[roomNumber] = room;
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
        #endregion
        public CancellationToken Token;

        //-- 유저 정보 캐싱 : 상태, 방번호, 이름, 착용아이템 등..
        public ConcurrentDictionary<string, UserInfo> Users = new ConcurrentDictionary<string, UserInfo>();
        //-- 생성된 채팅방 정보 캐싱 : 방 제목, 방장 이름, 입장 유저 정보
        public ConcurrentDictionary<int, JoinRoomInfo> JoinRooms = new ConcurrentDictionary<int, JoinRoomInfo>();
        public ConcurrentQueue<ChatBase.Packet>? ChatMessages = new ConcurrentQueue<ChatBase.Packet>();
        public SemaphoreSlim ChatSemaphore = new SemaphoreSlim(0);  // SemaphoreSlim을 사용하여 대기 상태 관리

        #region Http Server
        //-- 채팅방 유저정보 가져오기
        public JoinRoomUserInfo GetRoomUserInfo(string userName)
        {
            var roomUser = new JoinRoomUserInfo();
            var user = Users[userName];
            roomUser.UserName = userName;
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
        //-- 생성된 채팅방 목록 가져오기 : 방제목이 null이 아닌 채팅방만 반환
        public List<JoinRoomInfo> GetRoomList()
        {
            var roomList = new List<JoinRoomInfo>();
            foreach (var room in JoinRooms)
            {
                if (room.Value.RoomName == null) continue;
                roomList.Add(room.Value);
            }

            return roomList;
        }
        #endregion

        #region Tcp Server
        //-- 연결된 클라이언트 정보 캐싱 : 유저 명, 클라이언트
        public ConcurrentDictionary<string, TcpClient> Clients = new ConcurrentDictionary<string, TcpClient>();




        #endregion
    }
}


