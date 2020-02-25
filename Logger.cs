using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace burritocli
{
    internal static class Logger
    {
        public static void Log(string msg)
        {
            if (msg.StartsWith("[ERR]")) { Console.ForegroundColor = ConsoleColor.Red; }
            if (msg.StartsWith("[WARN]")) { Console.ForegroundColor = ConsoleColor.Yellow; }

            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Exit(string msg)
        {
            Console.WriteLine(msg);
            Environment.Exit(-1);
        }
    }
}
