using System;
using System.Collections.Generic;
using System.Linq;

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

    public static char MessageSeparator = ((char)007);
    public static event OnCommandProcessingDelegate OnCommandReceived;

    public static void SetupEvents()
    {
        PipeClient.OnMessageReceived += ReceiveCommand;
    }

    public static void SendCommand(CommandType command, string[] args)
    {
        if (!PipeClient.IsConnected()) return;

        List<string> argsToSend = new List<string> { command.ToString() };
        argsToSend.AddRange(args);

        PipeClient.SendMessage(string.Join(MessageSeparator.ToString(), argsToSend));
    }

    private static void ReceiveCommand(string msg)
    {
        try
        {
            string[] splitParts = msg.Split(MessageSeparator);
            CommandType type = (CommandType)Enum.Parse(typeof(CommandType), splitParts[0]);
            string[] args = splitParts.ToList().GetRange(1, splitParts.Length - 1).ToArray();

            if (OnCommandReceived != null)
                OnCommandReceived(type, args);
        }
        catch { }
    }
}