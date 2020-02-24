using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// Logs things to the built in burrito logger.
    /// </summary>
    public static class Logger
    {
        //The logger to write actions to a log file/console. Defaultly set to only throw.
        internal static Action<string> Log { get; set; } = (string s) => 
        { 
            if (s.StartsWith("[ERR]")) { throw new Exception(s); }
        };

        //The verbosity of the logger.
        public static int Verbosity { get; set; }

        /// <summary>
        /// Writes to the log if the verbosity level allows.
        /// </summary>
        public static void Write(string msg, int verbosity)
        {
            if (verbosity > Verbosity) { return; }
            Log(msg);
        }
    }
}
