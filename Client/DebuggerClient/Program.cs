using System;

namespace DebuggerClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SetupEventHandlers();
            PipeClient.Connect();

            Console.WriteLine("Pipe connected : " + PipeClient.IsConnected());

            string SeedStr =
                "2~2~1~0~0~24~25~0~False~2~False~5~0~0~HERO_04*-1*0*0*0*0*0*4*30*0*2*30*0*False*False*False*False*False*False*False*False*False*False*False*False*False*2*False~HERO_01*-1*2*0*0*0*0*36*30*0*2*32*0*False*False*False*False*False*False*False*False*False*False*False*False*False*2*False~CS2_101*-1*0*0*2*0*0*5*0*0*2*0*0*False*False*False*False*False*False*False*False*False*False*False*False*False*2*False~CS2_102*-1*0*0*2*0*0*37*0*0*2*0*0*False*False*False*False*True*False*False*False*False*False*False*False*False*2*False~EX1_080*0*0*1*1*0*0*9*2*0*1*2*0*False*False*False*False*False*False*False*False*False*False*False*False*False*1*False~0~GVG_061*0*0*0*3*0*0*24*0*0*0*0*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False|AT_079*1*0*6*6*0*0*20*6*0*0*6*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False|GVG_096*2*0*4*4*0*0*7*3*0*0*3*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False|GAME_005*3*0*0*0*0*0*68*0*0*0*0*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False|EX1_136*4*0*0*1*0*0*10*0*0*0*0*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False|AT_073*5*0*0*1*0*0*32*0*0*0*0*0*False*False*False*False*False*False*True*False*False*False*False*False*False*0*False~0~0~0~0~0~False=False=False=False=False=False~0~0~0~False~GVG_096,GVG_096,GVG_059,CS2_092,CS2_092,EX1_383,FP1_002,GVG_110,AT_073,AT_073,EX1_379,EX1_080,EX1_080,FP1_030,EX1_136,FP1_012,EX1_130,EX1_130,NEW1_019,NEW1_019,FP1_020,FP1_020,GVG_058,GVG_058,GVG_061,GVG_061,AT_079,AT_079,CS2_093,CS2_097~";
            string ProfileStr = "Default";
            bool AoESpells = true;


            SendSeedRequest(SeedStr, ProfileStr, AoESpells);

            Console.ReadLine();

            PipeClient.Disconnect();
        }

        private static void SetupEventHandlers()
        {
            CommandHandler.SetupEvents();
            CommandHandler.OnCommandReceived += CommandHandlerOnOnCommandReceived;
        }

        private static void CommandHandlerOnOnCommandReceived(CommandHandler.CommandType command, string[] args)
        {
            switch (command)
            {
                case CommandHandler.CommandType.ActionList:
                    Console.WriteLine("Actions list : " + string.Join(Environment.NewLine, args));
                    break;

                case CommandHandler.CommandType.BoardAfterActions:
                    //Console.WriteLine("BoardAfterActions : " + string.Join(Environment.NewLine, args));
                    break;

                case CommandHandler.CommandType.BoardBeforeActions:
                    //Console.WriteLine("BoardBeforeActions : " + string.Join(Environment.NewLine, args));
                    break;

                case CommandHandler.CommandType.Log:
                    Console.WriteLine("Log : " + string.Join(Environment.NewLine, args));
                    break;
            }
        }

        private static void SendSeedRequest(string seed, string profile, bool aoespells)
        {
            CommandHandler.SendCommand(CommandHandler.CommandType.CalculationRequest,
                new[] {seed, profile, aoespells.ToString()});
        }
    }
}