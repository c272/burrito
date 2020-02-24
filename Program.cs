using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Burrito;

namespace burritocli
{
    class Program
    {
        /// <summary>
        /// Entrypoint for the program.
        /// </summary>
        static void Main(string[] args)
        {
            //Create locals.
            var argMan = new ArgumentParser(args);
            var burrito = new BurritoAPI();
            burrito.SetLogger(Logger.Log);

            //Parse the arguments provided, set the runtime information.
            burrito.GenerationPath = argMan.GetValue("p") == null ? Environment.CurrentDirectory : argMan.GetValue("p");
            burrito.CompileAfterGeneration = argMan.GetFlag("c");
            burrito.APISchemaPath = argMan.GetValue("s");

            //Validate arguments.
            if (argMan.GetValue("s") == null)
            {
                Logger.Log("Missing an API schema definition to generate from (usage is '-s [path]').");
                Logger.Exit("For information on how to create a schema, see MANUAL.md.");
                return;
            }

            //Schema exists?
            if (!File.Exists(burrito.APISchemaPath))
            {
                Logger.Exit("Invalid file path given for schema, file does not exist.");
                return;
            }

            //Generating directory exists?
            if (!Directory.Exists(burrito.GenerationPath))
            {
                //Create it.
                try
                {
                    Directory.CreateDirectory(burrito.GenerationPath);
                }
                catch (Exception e)
                {
                    Logger.Exit("Invalid directory path given, '" + e.Message + "'.");
                    return;
                }
            }

            //Directory has any files in it?
            if (Directory.GetFiles(burrito.GenerationPath).Count() != 0)
            {
                //Use a child directory of the name of the schema file.
                string newDir = Path.Combine(burrito.GenerationPath, Path.GetFileNameWithoutExtension(burrito.APISchemaPath));
                Directory.CreateDirectory(newDir);
                burrito.GenerationPath = newDir;
            }

            //Run burrito.
            var t = new Stopwatch();
            t.Start();
            int routesGenerated = burrito.Run();
            t.Stop();

            //Write the finish message.
            Console.Write("Rolled up the API schema in ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(t.ElapsedMilliseconds / 1000f + "s");
            Console.ResetColor();
            Console.Write(", " + routesGenerated + " routes generated.\n");
        }
    }
}
