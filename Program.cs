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
            BurritoAPI.SetLogger(Logger.Log);

            //Parse the arguments provided, set the runtime information.
            BurritoAPI.GenerationPath = argMan.GetValue("p") == null ? Environment.CurrentDirectory : argMan.GetValue("p");
            BurritoAPI.APISchemaPath = argMan.GetValue("s");
            BurritoAPI.CompileMode = argMan.GetFlag("c") || argMan.GetFlag("dll");
            BurritoAPI.IncludeDebugInformation = argMan.GetFlag("debug");
            BurritoAPI.FollowNamingConventions = !argMan.GetFlag("nnc");
            BurritoAPI.GenerateAsyncAndSync = argMan.GetFlag("aas");
            BurritoAPI.GenerateNuspec = argMan.GetFlag("nuspec");

            //Try to parse verbosity level.
            string verbosityLvl = argMan.GetValue("v");
            int vblvl;
            if (verbosityLvl != null && int.TryParse(verbosityLvl, out vblvl))
            {
                BurritoAPI.VerbosityLevel = vblvl;
            }

            //Validate arguments.
            if (argMan.GetValue("s") == null)
            {
                //No schema, attempt to find one in the local folder.
                var files = Directory.GetFiles(Environment.CurrentDirectory);
                List<FileInfo> jsons = new List<FileInfo>();
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.Extension == ".json")
                    {
                        jsons.Add(fi);
                    }
                }

                //Find the first valid JSON with Burrito.
                string validSchemaLoc = null;
                foreach (var file in jsons)
                {
                    if (BurritoAPI.IsValidSchema(file.FullName))
                    {
                        validSchemaLoc = file.FullName;
                    }
                }

                //Found one?
                if (validSchemaLoc == null)
                {
                    Logger.Log("Missing an API schema definition to generate from (usage is '-s [path]').");
                    Logger.Exit("For information on how to create a schema, see the wiki.");
                    return;
                }

                //Yes, set path.
                BurritoAPI.APISchemaPath = validSchemaLoc;
            }

            //Schema exists?
            if (!File.Exists(BurritoAPI.APISchemaPath))
            {
                Logger.Exit("Invalid file path given for schema, file does not exist.");
                return;
            }

            //Generating directory exists?
            if (!Directory.Exists(BurritoAPI.GenerationPath))
            {
                //Create it.
                try
                {
                    Directory.CreateDirectory(BurritoAPI.GenerationPath);
                }
                catch (Exception e)
                {
                    Logger.Exit("Invalid directory path given, '" + e.Message + "'.");
                    return;
                }
            }

            //Directory has any files in it, and not compiling?
            if (!BurritoAPI.CompileMode && Directory.GetFiles(BurritoAPI.GenerationPath).Count() != 0)
            {
                //Use a child directory of the name of the schema file.
                string newDir = Path.Combine(BurritoAPI.GenerationPath, Path.GetFileNameWithoutExtension(BurritoAPI.APISchemaPath));
                Directory.CreateDirectory(newDir);
                BurritoAPI.GenerationPath = newDir;
            }

            //Run burrito.
            Logger.Log("Rolling up schema '" + new FileInfo(BurritoAPI.APISchemaPath).Name + "'...");
            var t = new Stopwatch();
            t.Start();
            int filesGenerated = BurritoAPI.Run();
            t.Stop();

            //Write the finish message.
            Console.Write("Rolled up the API schema in ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(t.ElapsedMilliseconds / 1000f + "s");
            Console.ResetColor();
            Console.Write(", " + filesGenerated + " classes generated.\n");
        }
    }
}
