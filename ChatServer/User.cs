
using System.Net.Sockets;

public class User
{
    public User(TcpClient client, string userName, int state, int roomNumber)
    {
        Client = client;
        UserName = userName;
        State = state;
        RoomNumber = roomNumber;
        IsAlive = false;
    }

    public TcpClient? Client { get; set; }
    public string? UserName { get; set; }
    public int State { get; set; }
    public int RoomNumber { get; set; }

    public bool IsAlive { get; set; }
}