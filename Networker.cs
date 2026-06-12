using System.Net;
using System.Net.Sockets;
using System.Text;

public class Networker
{
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;

    public Networker(string username, string ip, int port)
    {
        Console.WriteLine("Trying to connect as " + username);

        TcpClient? tcpClient = null;
        
        try
        {
            tcpClient = new(ip, port);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }

        Console.WriteLine("Connected to server!");

        if (tcpClient == null) throw new Exception("Couldn't connect to server: Server not found :(");

        _tcpClient = tcpClient;
        _networkStream = tcpClient.GetStream();
        
        byte[] buffer = Encoding.UTF8.GetBytes($"Lolski I am zhe {username}");
        byte[] packet = new byte[2 + buffer.Length];

        packet[0] = (byte)(buffer.Length + 1);
        packet[1] = 0x00;

        buffer.CopyTo(packet, 2);
        
        _networkStream.WriteAsync(packet);

        _ = Task.Run(async () => ReceivePackets());
    }

    public void CloseConnection()
    {
        _networkStream.Close();
        _tcpClient.Close();
    }

    private async Task ReceivePackets()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
            {
                Console.WriteLine("Disconnected from server D:");
                CloseConnection();
                return;
            }

            Console.Write($"Received packet [Id:{buffer[1]}]: ");

            Console.WriteLine(Encoding.UTF8.GetString(buffer, 2, bytesRead - 2));
            buffer = new byte[1024];
        }
    }
}