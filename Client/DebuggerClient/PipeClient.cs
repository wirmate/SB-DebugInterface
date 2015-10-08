using System;
using System.IO.Pipes;
using System.Text;

public static class PipeClient
{
    public delegate void OnMessageReceivedDelegate(string msg);

    private static NamedPipeClientStream _clientStream;

    private static byte[] headerBuffer = new byte[sizeof (int)];
    private static byte[] messageBuffer = new byte[1];
    public static event OnMessageReceivedDelegate OnMessageReceived;

    public static bool IsConnected()
    {
        if (_clientStream == null) return false;
        return _clientStream.IsConnected;
    }

    public static void SendMessage(string str)
    {
        if (!IsConnected()) return;
        SendBytes(Encoding.Unicode.GetBytes(str));
    }

    private static void SendBytes(byte[] bytes)
    {
        if (_clientStream != null)
        {
            byte[] bytesSize = BitConverter.GetBytes(bytes.Length);
            byte[] bytesToSend = new byte[bytes.Length + sizeof (int)];

            Buffer.BlockCopy(bytesSize, 0, bytesToSend, 0, bytesSize.Length);
            Buffer.BlockCopy(bytes, 0, bytesToSend, sizeof (int), bytes.Length);

            _clientStream.BeginWrite(bytesToSend, 0, bytesToSend.Length,
                delegate(IAsyncResult ar) { _clientStream.EndWrite(ar); }, null);
        }
    }

    public static bool Connect()
    {
        if (IsConnected()) return false;

        NamedPipeClientStream client = new NamedPipeClientStream(".", "SmartBotDebuggerPipe", PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            client.Connect(2000);
        }
        catch
        {
            return false;
        }

        _clientStream = client;
        BeginReceiveHeader();

        return true;
    }

    public static void Disconnect()
    {
        if (!IsConnected()) return;

        _clientStream.Dispose();
        _clientStream = null;
    }

    private static void BeginReceiveHeader()
    {
        try
        {
            if (_clientStream != null && _clientStream.IsConnected)
            {
                headerBuffer = new byte[sizeof (int)];
                _clientStream.BeginRead(headerBuffer, 0, sizeof (int), delegate(IAsyncResult ar)
                {
                    try
                    {
                        _clientStream.EndRead(ar);
                        int msgSize = BitConverter.ToInt32(((byte[]) ar.AsyncState), 0);
                        BeginReceiveMessage(msgSize);
                    }
                    catch
                    {
                        Disconnect();
                    }
                }, headerBuffer);
            }
        }
        catch
        {
            Disconnect();
        }
    }

    private static void BeginReceiveMessage(int size)
    {
        try
        {
            if (_clientStream != null && _clientStream.IsConnected)
            {
                messageBuffer = new byte[size];
                _clientStream.BeginRead(messageBuffer, 0, size, delegate(IAsyncResult ar)
                {
                    try
                    {
                        _clientStream.EndRead(ar);
                        string message = Encoding.Unicode.GetString((byte[]) ar.AsyncState);
                        HandleMessage(message);

                        BeginReceiveHeader();
                    }
                    catch
                    {
                        Disconnect();
                    }
                }, messageBuffer);
            }
        }
        catch
        {
            Disconnect();
        }
    }

    private static void HandleMessage(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return;
        if (OnMessageReceived != null)
            OnMessageReceived(str);
    }
}