
namespace Util
{
    enum State
    {
        Lobby,
        Room,
        Shop,
        BeautyRoom,
        Logout
    }
    enum Opcode
    {
        Ping,
        Chat,
        JoinGame, //-- 게임 접속 (서버<->클라이언트)
        AddUserLobbyMember,
        AddChatRoomLobbyMember,
        RoomLobbyMember,
        LobbyMember,
        JoinRoomMember,
        ExitRoomMember,
        Logout,
    }
}
