using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using SmartBot.Plugins.API;

namespace SmartBot.Plugins
{
    [Serializable]
    public class bPluginDataContainer : PluginDataContainer
    {
        public bPluginDataContainer()
        {
            Name = "DebuggerInterface";
        }
    }

    public class DebuggerInterfacePlugin : Plugin
    {
        public override void OnPluginCreated()
        {
            Bot.Log("DebuggerInterface created");

            PipeServer.Start();

            CommandHandler.SetupEvents();
            SetupEventReveivers();
        }

        private void SetupEventReveivers()
        {
            Debug.OnActionsReceived += DebugOnOnActionsReceived;
            Debug.OnAfterBoardReceived += DebugOnOnAfterBoardReceived;
            Debug.OnBeforeBoardReceived += DebugOnOnBeforeBoardReceived;
            Debug.OnLogReceived += DebugOnOnLogReceived;

            CommandHandler.OnCommandReceived += CommandHandlerOnCommandReceived;

            Bot.Log("DebuggerInterface events setup");
        }

        private void CommandHandlerOnCommandReceived(CommandHandler.CommandType command, string[] args)
        {
            if (command == CommandHandler.CommandType.CalculationRequest)
                Debug.SimulateSeed(args[0], args[1], true);
        }

        private void DebugOnOnLogReceived(string str)
        {
            CommandHandler.SendCommand(CommandHandler.CommandType.Log, new[] {str});
        }

        private void DebugOnOnBeforeBoardReceived(string str)
        {
            CommandHandler.SendCommand(CommandHandler.CommandType.BoardBeforeActions, new[] {str});
        }

        private void DebugOnOnAfterBoardReceived(string str)
        {
            CommandHandler.SendCommand(CommandHandler.CommandType.BoardAfterActions, new[] {str});
        }

        private void DebugOnOnActionsReceived(string str)
        {
            CommandHandler.SendCommand(CommandHandler.CommandType.ActionList, new[] {str});
        }

        ~DebuggerInterfacePlugin()
        {
            PipeServer.Close();
        }
    }

    public static class CommandHandler
    {
        public delegate void OnCommandProcessingDelegate(CommandType command, string[] args);

        public enum CommandType
        {
            Log,
            CalculationRequest,
            ActionList,
            BoardBeforeActions,
            BoardAfterActions
        }

        public static char MessageSeparator = ((char) 007);
        public static event OnCommandProcessingDelegate OnCommandReceived;

        public static void SetupEvents()
        {
            PipeServer.OnMessageReceived += ReceiveCommand;
        }

        public static void SendCommand(CommandType command, string[] args)
        {
            if (!PipeServer.Connected) return;

            List<string> argsToSend = new List<string> {command.ToString()};
            argsToSend.AddRange(args);

            PipeServer.SendMessage(string.Join(MessageSeparator.ToString(), argsToSend));
        }

        private static void ReceiveCommand(string msg)
        {
            try
            {
                string[] splitParts = msg.Split(MessageSeparator);
                CommandType type = (CommandType) Enum.Parse(typeof (CommandType), splitParts[0]);
                string[] args = splitParts.ToList().GetRange(1, splitParts.Length - 1).ToArray();

                if (OnCommandReceived != null)
                    OnCommandReceived(type, args);
            }
            catch {}
        }
    }

    public static class PipeServer
    {
        public delegate void OnMessageReceivedDelegate(string msg);

        public static volatile bool Connected;
        private static Thread _pipeServerThread;
        private static NamedPipeServerStream _client;

        private static byte[] headerBuffer = new byte[sizeof (int)];
        private static byte[] messageBuffer = new byte[1];
        public static event OnMessageReceivedDelegate OnMessageReceived;

        public static void Start()
        {
            if (Connected) Close();

            Connected = true;
            Bot.Log("DebuggerInterface - SmartBotDebuggerPipe -> Start()");

            _pipeServerThread = new Thread(WaitForClient);
            _pipeServerThread.Start();
        }


        private static void WaitForClient()
        {
            while (Connected)
            {
                if (_client == null || _client.IsConnected == false)
                {
                    CloseClient();
                    _client = new NamedPipeServerStream("SmartBotDebuggerPipe", PipeDirection.InOut, 2,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    _client.WaitForConnection();
                    BeginReceiveHeader();
                }
            }
        }

        private static void BeginReceiveHeader()
        {
            try
            {
                if (_client != null && _client.IsConnected)
                {
                    headerBuffer = new byte[sizeof (int)];
                    _client.BeginRead(headerBuffer, 0, sizeof (int), delegate(IAsyncResult ar)
                    {
                        try
                        {
                            _client.EndRead(ar);
                            int msgSize = BitConverter.ToInt32(((byte[]) ar.AsyncState), 0);
                            BeginReceiveMessage(msgSize);
                        }
                        catch
                        {
                            CloseClient();
                        }
                    }, headerBuffer);
                }
            }
            catch
            {
                CloseClient();
            }
        }

        private static void BeginReceiveMessage(int size)
        {
            try
            {
                if (_client != null && _client.IsConnected)
                {
                    messageBuffer = new byte[size];
                    _client.BeginRead(messageBuffer, 0, size, delegate(IAsyncResult ar)
                    {
                        try
                        {
                            _client.EndRead(ar);
                            string message = Encoding.Unicode.GetString((byte[]) ar.AsyncState);
                            HandleMessage(message);

                            BeginReceiveHeader();
                        }
                        catch
                        {
                            CloseClient();
                        }
                    }, messageBuffer);
                }
            }
            catch
            {
                CloseClient();
            }
        }

        public static void SendMessage(string str)
        {
            if (_client != null && _client.IsConnected)
            {
                if (string.IsNullOrWhiteSpace(str)) return;
                try
                {
                    SendBytes(Encoding.Unicode.GetBytes(str));
                }
                catch (Exception e)
                {
                    Bot.Log("DebuggerInterface -> Error sending message : " + str + Environment.NewLine + e);
                }
            }
        }

        private static void SendBytes(byte[] bytes)
        {
            if (_client != null)
            {
                byte[] bytesSize = BitConverter.GetBytes(bytes.Length);
                byte[] bytesToSend = new byte[bytes.Length + sizeof (int)];

                Buffer.BlockCopy(bytesSize, 0, bytesToSend, 0, bytesSize.Length);
                Buffer.BlockCopy(bytes, 0, bytesToSend, sizeof (int), bytes.Length);

                _client.BeginWrite(bytesToSend, 0, bytesToSend.Length,
                    delegate(IAsyncResult ar) { _client.EndWrite(ar); }, null);
            }
        }

        private static void HandleMessage(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return;

            if (OnMessageReceived != null)
                OnMessageReceived(str);
        }

        public static void CloseClient()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client.Dispose();
                _client = null;
            }
        }

        public static void Close()
        {
            CloseClient();
            Connected = false;
            if (_pipeServerThread != null)
                _pipeServerThread.Join(10000);
        }
    }
}