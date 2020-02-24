using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace burrito
{
    class Program
    {
        /// <summary>
        /// Entrypoint for the program.
        /// </summary>
        static void Main(string[] args)
        {
            //Parse the arguments provided, set the runtime information.
            var argMan = new ArgumentParser(args);
            RuntimeInformation.GenerationPath = argMan.GetValue("p") == null ? Environment.CurrentDirectory : argMan.GetValue("p");
            RuntimeInformation.CompileAfterGeneration = argMan.GetFlag("c");
            if (argMan.GetValue("s") == null)
            {
                Error.Exit("Missing an API schema definition to generate from (usage is '-s [path]').");
                return;
            }

            //todo

        }
    }
}
