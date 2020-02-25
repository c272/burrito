using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILRepacking;
using System.Reflection;

namespace Burrito
{
    /// <summary>
    /// Represents an entire project's worth of code.
    /// </summary>
    public class ProjectCode
    {
        public Dictionary<string, string> Files = new Dictionary<string, string>();
        public List<string> Dependencies = new List<string>()
        {
            "Newtonsoft.Json.dll", //json libs
            Assembly.GetExecutingAssembly().Location //this asm
        };
        public string ProjectName;

        public ProjectCode(string name)
        {
            ProjectName = name;
        }

        /// <summary>
        /// Compiles this project to a DLL file.
        /// </summary>
        public void CompileToDLL(string generationPath)
        {
            //Set up compilation.
            string outputPath = Path.Combine(generationPath, ProjectName + ".dll");
            string pdbPath = Path.Combine(generationPath, ProjectName + ".pdb");

            //Generate syntax trees for all files.
            var trees = new List<SyntaxTree>();
            foreach (var file in Files.Values)
            {
                try
                {
                    trees.Add(CSharpSyntaxTree.ParseText(file));
                }
                catch (Exception e)
                {
                    Logger.Write("[ERR] - Internal compile error: '" + e.Message + "'.", 1);
                    return;
                }
            }

            //Generate dependency references.
            var deps = new List<PortableExecutableReference>();
            deps.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)); //make sure to add system
            deps.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)); //linq
            foreach (var dep in Dependencies)
            {
                deps.Add(MetadataReference.CreateFromFile(dep));
            }

            //Set up compilation.
            CSharpCompilation compilation = CSharpCompilation.Create(
                ProjectName,
                trees.ToArray(),
                deps.ToArray(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            //Write to a DLL stream.
            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    //Emit errors.
                    foreach (var error in emitResult.Diagnostics)
                    {
                        Logger.Write("[ERR] - Internal compile error at '" + error.Location + "': " + error.GetMessage(), 1);
                    }
                    return;
                }

                //From the DLL stream, write to file.
                using (FileStream fs = new FileStream(outputPath, FileMode.OpenOrCreate))
                {
                    dllStream.WriteTo(fs);
                    fs.Flush();
                }

                //If debug information is enabled, flush PDB too.
                if (BurritoAPI.IncludeDebugInformation)
                {
                    using (FileStream fs = new FileStream(pdbPath, FileMode.OpenOrCreate))
                    {
                        pdbStream.WriteTo(fs);
                        fs.Flush();
                    }
                }
            }

            //Set up options for merging DLL libraries (suppressing console input).
            RepackOptions opt = new RepackOptions();
            opt.OutputFile = outputPath + ".packed";
            opt.SearchDirectories = new string[] { Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory };

            //Setting input assemblies.
            string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            opt.InputAssemblies = new string[] { outputPath }.Concat(Dependencies).ToArray();

            //Redirecting console input temporarily.
            using (var writer = new ConsoleCatcher())
            {
                //Set up writer.
                var originalOut = Console.Out;
                if (BurritoAPI.VerbosityLevel < 3)
                {
                    Console.SetOut(writer);
                }

                //Merge.
                ILRepack pack = new ILRepack(opt);
                pack.Repack();

                //Set input back.
                Console.SetOut(originalOut);
            }

            //Delete original assembly, rename packed.
            string packedName = outputPath + ".packed";
            try
            {
                File.Delete(outputPath);
                File.Move(packedName, outputPath);
            }
            catch
            {
                Logger.Write("[WARN] - Failed renaming packed (.dll.packed) to normal (.dll), insufficient permissions. Job still completed.", 1);
            }
        }

        internal void CompileToProject(string generationPath)
        {
            throw new NotImplementedException();
        }
    }
}