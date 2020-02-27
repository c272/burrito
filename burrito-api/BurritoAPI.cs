using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// Represents a single instance of the Burrito API.
    /// </summary>
    public static class BurritoAPI
    {
        //What path to generate the library at.
        public static string GenerationPath { get; set; } = Environment.CurrentDirectory;

        //Where to draw the API schema from to use.
        public static string APISchemaPath { get; set; } = null;

        //Whether to only compile the DLL and ignore writing to project.
        public static bool CompileMode { get; set; } = false;

        //Whether the program should generate fields following C# naming conventions.
        public static bool FollowNamingConventions { get; set; }
        
        //Whether to include debug information or not when compiling.
        public static bool IncludeDebugInformation { get; set; } = false;

        //Whether to generate asynchronous AND synchronous methods.
        public static bool GenerateAsyncAndSync { get; set; } = false;

        //Whether to generate a NuGet specification for the project.
        public static bool GenerateNuspec { get; set; } = false;

        //The current project being compiled.
        public static ProjectModule Project { get; set; } = null;

        //The verbosity level of the generator.
        //0 - No logging.
        //1 - Critical logging only (errors).
        //2 - Normal logging (status updates & errors).
        //3 - Debug logging.
        public static int VerbosityLevel
        {
            get { return Logger.Verbosity; }
            set { Logger.Verbosity = value; }
        }

        /// <summary>
        /// Run the currently set up instance of Burrito.
        /// </summary>
        public static int Run()
        {
            //Try and deserialize the schema.
            APISchema schema;
            try
            {
                schema = JsonConvert.DeserializeObject<APISchema>(File.ReadAllText(APISchemaPath));
            }
            catch (Exception e)
            {
                Logger.Write("[ERR] - Failed to load API schema, '" + e.Message + "'.", 1);
                return 0;
            }

            //All relevant properties exist?
            if (schema.Sections == null || schema.Name == null || schema.RootPath == null)
            {
                Logger.Write("[ERR] - Required chema property missing (one of 'name', 'root' or 'sections').", 1);
                return 0;
            }

            //Valid schema name?
            if (!Regex.IsMatch(schema.Name, "^[A-Za-z0-9_]+$"))
            {
                Logger.Write("[ERR] - Invalid schema name given. Must only be alphanumeric or underscores.", 1);
                return 0;
            }

            //If the root path doesn't end with a "/", add it on.
            if (!schema.RootPath.EndsWith("/"))
            {
                schema.RootPath += "/";
            }

            //Create a project module with the given schema and namespaces.
            Project = new ProjectModule(schema.Name, schema.RootPath);
            Project.AddNamespace("Data");
            Project.AddNamespace("@");
            Project.Namespaces["Data"].Add(new ClassModule("_empty"));

            //Check all the sections in the schema have unique names.
            var sectionNames = new List<string>();
            foreach (var module in schema.Sections)
            {
                if (sectionNames.Contains(module.Name))
                {
                    Logger.Write("[ERR] - Duplicate section name '" + module.Name + "' detected.", 1);
                    return 0;
                }
                sectionNames.Add(module.Name);
            }

            //Loop through the routes and generate code modules for each of them.
            foreach (var module in schema.Sections)
            {
                var moduleClass = new ClassModule(module.Name);
                foreach (var route in module.Routes)
                {
                    //Check whether the route has valid properties.
                    if (!route.Validate())
                    {
                        return 0;
                    }

                    //What method does this route use?
                    switch (route.HTTPMethod)
                    {
                        case null:
                            Logger.Write("[ERR] - No HTTP method defined for route '" + route.RelativeURL + "'.", 1);
                            return 0;
                        case "GET":
                        case "get":
                            //Figure out a data type from the API endpoint (if possible).
                            bool isList = false;
                            var classReturned = DataTypeCreator.DeriveFromRoute(schema.RootPath, route, ref isList);
                            if (classReturned == null) { break; }

                            //Add class.
                            Project.Namespaces["Data"].Add(classReturned);

                            //Add the method.
                            moduleClass.Methods.Add(new GETMethodModule()
                            {
                                Async = route.Async,
                                Route = route.RelativeURL,
                                RouteParams = route.GetRouteVariables(),
                                XMLSummary = route.GetSummary(),
                                Name = route.GetMethodName(),
                                ReceivedDataType = classReturned,
                                ReturnsList = isList
                            });
                            break;
                        case "POST":
                        case "post":
                            //Figure out a data type it should receive.
                            bool returnsList = false;
                            var postClassReturned = DataTypeCreator.DeriveFromRoute(schema.RootPath, route, ref returnsList, route.ExampleData);
                            if (postClassReturned == null) { break; }

                            //Figure out the data type is should send.
                            if (route.ExampleData == null)
                            {
                                Logger.Write("[ERR] - Cannot implement POST route '" + route.RelativeURL + "' without example data to send.", 1);
                                break;
                            }
                            if (route.SentDataName == null)
                            {
                                Logger.Write("[ERR] - Sent data name not set for POST route '" + route.RelativeURL + "', skipping.", 1);
                                break;
                            }
                            var postClass = DataTypeCreator.FromJObject(route.ExampleData, route.SentDataName);
                            if (postClass == null) { break; }

                            //Add classes.
                            Project.Namespaces["Data"].Add(postClassReturned);
                            Project.Namespaces["Data"].Add(postClass);

                            //Add the method.
                            moduleClass.Methods.Add(new POSTMethodModule()
                            {
                                Async = route.Async,
                                Route = route.RelativeURL,
                                XMLSummary = route.GetSummary(),
                                RouteParams = route.GetRouteVariables(),
                                SentDataType = postClass,
                                ReceivedDataType = postClassReturned,
                                Name = route.GetMethodName(),
                                ReturnsList = returnsList
                            });
                            break;
                    }
                }

                //Add the class to root namespace.
                Project.Namespaces["@"].Add(moduleClass);
            }

            //All classes generated, generate code for the project.
            ProjectCode code = Project.GenerateCode();

            //Decide what to do with it.
            if (CompileMode)
            {
                code.CompileToDLL(GenerationPath);
            }
            else
            {
                try
                {
                    code.CompileToProject(GenerationPath);
                }
                catch (Exception e)
                {
                    Logger.Write("[ERR] - Error writing project to disk: '" + e.Message + "'.", 1);
                }
            }

            //If a NuSpec needs to be generated, do it now.
            if (GenerateNuspec)
            {
                code.GenerateNuspec(GenerationPath);
            }

            return code.Files.Count;
        }

        /// <summary>
        /// Sets the logger used by Burrito to the provided method.
        /// </summary>
        public static void SetLogger(Action<string> logger)
        {
            Logger.Log = logger;
        }
    }
}
