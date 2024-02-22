using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using GameInfo;
using Newtonsoft.Json;
using Util;

namespace server
{
    public class ServerManager
    {
        #region Singleton
        private static ServerManager? instance;
        private ServerManager()
        {
            for (int roomNumber = 0; roomNumber < 100; roomNumber++)
            {
                var room = new Room();
                room!.RoomNumber = roomNumber;
                room.CurrentMember = 0;
                room.RoomName = "";
                room.OwnerName = "";
                Rooms[roomNumber] = room;
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
        public ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();
        public ConcurrentDictionary<string, TcpClient> Clients = new ConcurrentDictionary<string, TcpClient>();
        //-- 생성된 채팅방 정보 캐싱 : 방 제목, 방장 이름, 입장 유저 정보
        public ConcurrentDictionary<int, Room> Rooms = new ConcurrentDictionary<int, Room>();
        public ConcurrentQueue<ServerPacket.Packet>? ChatMessages = new ConcurrentQueue<ServerPacket.Packet>();
        public SemaphoreSlim ChatSemaphore = new SemaphoreSlim(0);  // SemaphoreSlim을 사용하여 대기 상태 관리

        #region Http Server
        //-- 채팅방 유저정보 가져오기
        public Character GetCharacter(string userName)
        {
            var user = Users[userName];
            var character = new Character();
            character!.UserName = user.UserName;
            character.SlotNumber = user.SlotNumber;
            character.Gender = user.Gender;
            character.Model = user.Model;
            character.Items = new List<int>();
            foreach (var item in user.Items!)
            {
                if (!item.Value) continue;
                character.Items.Add(item.Key);
            }
            return character;
        }
        #endregion
        public string GetOpcodeString(int opcode)
        {
            switch (opcode)
            {
                case (int)Opcode.JoinGame:
                    return "JoinGame";
                case (int)Opcode.AddUserLobbyMember:
                    return "AddUserLobbyMember";
                case (int)Opcode.AddChatRoomLobbyMember: //-- 본인을 제외한 로비에 위치한 유저들 가져옴
                    return "AddChatRoomLobbyMember";
                case (int)Opcode.RoomLobbyMember:
                    return "RoomLobbyMember";
                case (int)Opcode.LobbyMember:
                    return "LobbyMember";
                case (int)Opcode.JoinRoomMember:
                    return "JoinRoomMember";
                case (int)Opcode.ExitRoomMember:
                    return "ExitRoomMember";
                case (int)Opcode.Logout:
                    return "Logout";
                default:
                    return $"NotFound! [{opcode}]";
            }

        }
    }
}


