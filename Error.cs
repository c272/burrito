using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace burrito
{
    internal static class Error
    {
        public static void Exit(string msg)
        {
            Console.WriteLine(msg);
            Environment.Exit(-1);
        }
    }
}
