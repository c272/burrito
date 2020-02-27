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
        public Dictionary<ProjectFileInfo, string> Files = new Dictionary<ProjectFileInfo, string>();
        public List<string> CompileDependencies = new List<string>()
        {
            "Newtonsoft.Json.dll", //json libs
            "burrito-core.dll" //core
        };
        public List<string> ProjectAsmDeps = new List<string>()
        {
            "Newtonsoft.Json",
            "System",
            "System.Linq",
            "System.Data",
            "burrito-core"
        };

        //Project include assembly descriptions. Bool is for including a hint path.
        public Dictionary<string, bool> ProjectAsmDescriptions = new Dictionary<string, bool>()
        {
            {"burrito-core", true},
            { "Newtonsoft.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL", true },
            { "System", false },
            { "System.Core", false },
            { "System.Xml.Linq", false },
            { "Microsoft.CSharp", false },
            { "System.Data", false },
            { "System.Xml", false },
            { "System.Net.Http", false }
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
            deps.Add(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)); //make sure to add system
            deps.Add(MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location)); //linq
            deps.Add(MetadataReference.CreateFromFile(typeof(BurritoCore.API).GetTypeInfo().Assembly.Location)); //api
            foreach (var dep in CompileDependencies)
            {
                deps.Add(MetadataReference.CreateFromFile(dep));
            }

            //Compilation options.
            var compilationOpts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            //Set up compilation.
            CSharpCompilation compilation = CSharpCompilation.Create(
                ProjectName,
                trees.ToArray(),
                deps.ToArray(),
                compilationOpts);

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
            opt.InputAssemblies = new string[] { outputPath }.Concat(CompileDependencies).ToArray();

            //Redirecting console input temporarily.
            using (var writer = new ConsoleCatcher())
            {
                //Set up writer.
                var originalOut = Console.Out;
                if (BurritoAPI.VerbosityLevel < 0)
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
                Logger.Write("[WARN] - Failed renaming packed (.dll.packed) to normal (.dll), insufficient permissions. Job still completed.", 2);
            }
        }

        /// <summary>
        /// Compiles this project to a physical project on disk.
        /// </summary>
        public void CompileToProject(string generationPath)
        {
            //Generate all basic files, separated into folders by namespace.
            List<string> paths = new List<string>();
            foreach (var file in Files)
            {
                //Create the containing folder.
                Directory.CreateDirectory(file.Key.GetFolderString(generationPath));

                //Write to the file.
                string filePath = Path.Combine(file.Key.GetFolderString(generationPath), file.Key.ClassName + ".cs");
                paths.Add(filePath);
                File.WriteAllText(filePath, file.Value);
            }

            //Copy required DLLs to the Dependencies folder.
            string depsFolder = Path.Combine(generationPath, "Dependencies");
            Directory.CreateDirectory(depsFolder);
            foreach (var dll in ProjectAsmDeps)
            {
                try
                {
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll + ".dll"), Path.Combine(depsFolder, dll + ".dll"), true);
                }
                catch
                {
                    if (dll == "System" || dll == "System.Linq") { continue; } //ignore missing system.dll & linq
                    Logger.Write("[WARN] - Failed to copy dependency DLL '" + dll + "'. Reference left.", 2);
                }
            }

            //Write the .csproj file to include all source files.
            string csproj = "<Project ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" >";

            //MSBuild wizardry.
            csproj += "<Import Project=\"$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props\" Condition=\"Exists('$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props')\" />";

            //Add the configuration propertygroup.
            csproj += "<PropertyGroup>";
            csproj += "<Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>";
            csproj += "<GenerateDocumentationFile>true</GenerateDocumentationFile>";
            csproj += "<Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>";
            csproj += "<OutputType>Library</OutputType>";
            csproj += "<AssemblyName>" + ProjectName + "</AssemblyName>";
            csproj += "<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>";
            csproj += "</PropertyGroup>";

            //Add the build combinations for release and debug.
            csproj += "<PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">" +
                      "<PlatformTarget>AnyCPU</PlatformTarget><DebugSymbols>true</DebugSymbols><DebugType>full</DebugType>" +
                      "<Optimize>false</Optimize>" +
                      "<OutputPath>bin\\Debug\\</OutputPath>" +
                      "<DefineConstants>DEBUG;TRACE</DefineConstants>" +
                      "<ErrorReport>prompt</ErrorReport>" +
                      "<WarningLevel>4</WarningLevel>" +
                      "</PropertyGroup>" +
                      "<PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|AnyCPU'\">" +
                      "<PlatformTarget>AnyCPU</PlatformTarget>" +
                      "<DebugType>pdbonly</DebugType>" +
                      "<Optimize>true</Optimize>" +
                      "<OutputPath>bin\\Release\\</OutputPath>" +
                      "<DefineConstants>TRACE</DefineConstants>" +
                      "<ErrorReport>prompt</ErrorReport>" +
                      "<WarningLevel>4</WarningLevel>" +
                      "</PropertyGroup>";

            //Add the references.
            csproj += "<ItemGroup>";
            foreach (var dep in ProjectAsmDescriptions)
            {
                csproj += "<Reference Include=\"" + dep.Key + "\" >";
                if (dep.Value)
                {
                    csproj += "<HintPath>Dependencies\\" + dep.Key + ".dll</HintPath>";
                }
                csproj += "<SpecificVersion>False</SpecificVersion>";
                csproj += "</Reference>";
            }
            csproj += "</ItemGroup>";

            //Add files to include (local refs).
            csproj += "<ItemGroup>";
            foreach (var file in paths)
            {
                csproj += "<Compile Include=\"" + file.Substring(generationPath.Length + 1) + "\" />";
            }
            csproj += "</ItemGroup>";

            //Add the build target.
            csproj += "<Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />";

            //Close file.
            csproj += "</Project>";
            
            //Write the .csproj to file.
            string csprojLoc = Path.Combine(generationPath, ProjectName + ".csproj");
            File.WriteAllText(csprojLoc, csproj);
        }

        /// <summary>
        /// Generates a NuGet specification for the project. at the generation path.
        /// </summary>
        public void GenerateNuspec(string generationPath)
        {
            //Add generic template information.
            string nuspec = "";
            nuspec += @"<?xml version=""1.0"" encoding=""utf - 8""?>";
            nuspec += "<package>";
            nuspec += "<metadata>";
            nuspec += "<id>$id$</id><version>$version$</version><title>$title$</title><authors>$author$</authors><owners>$author$</owners>";
            nuspec += "<requireLicenseAcceptance>false</requireLicenseAcceptance>";
            nuspec += "<license type=\"expression\">MIT</license>";

            //URL and logo of project.
            nuspec += "<projectUrl>{Your project page URL here.}</projectUrl>";
            nuspec += "<icon>{Your icon file reference here.}</icon>";

            //Description.
            nuspec += "<description>$description$</description>";

            //Release notes & copyright.
            nuspec += "<releaseNotes>{Your latest release changelog here.}</releaseNotes>";
            nuspec += "<copyright>(c) {Name} {Year}</copyright>";
            nuspec += "<tags>{Space separated tags here.}</tags>";

            //Dependencies.
            nuspec += "<dependencies><group targetFramework=\".NETFramework4.7.1\">";
            nuspec += "<dependency id=\"Newtonsoft.Json\" version=\"12.0.0.0\"/>";
            nuspec += "</group></dependencies>";
            nuspec += "</metadata>";

            //Files section.
            nuspec += "<files>";
            nuspec += "<file src=\"{Your relative logo path here.}\" target=\"\" />";
            nuspec += "</files>";

            nuspec += "</package>";

            //Write the .nuspec to file.
            File.WriteAllText(Path.Combine(generationPath, ProjectName + ".nuspec"), nuspec);
        }
    }

    /// <summary>
    /// Represents information about a given project file.
    /// </summary>
    public class ProjectFileInfo
    {
        public string Namespace;
        public string ClassName;
        public string GetFolderString(string basePath)
        {
            if (Namespace == "@") { return basePath; }
            return Path.Combine(basePath, Namespace);
        }
    }
}