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
    public class BurritoAPI
    {
        //Whether to compile the library after generating it.
        public bool CompileAfterGeneration { get; set; } = false;

        //What path to generate the library at.
        public string GenerationPath { get; set; } = Environment.CurrentDirectory;

        //Where to draw the API schema from to use.
        public string APISchemaPath { get; set; } = null;

        //The verbosity level of the generator.
        //0 - No logging.
        //1 - Critical logging only (errors).
        //2 - Normal logging (status updates & errors).
        //3 - Debug logging.
        public int VerbosityLevel
        {
            get { return Logger.Verbosity; }
            set { Logger.Verbosity = value; }
        }

        public int Run()
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

            //Create a project module with the given schema and namespaces.
            var project = new ProjectModule(schema.Name);
            project.AddNamespace("Data");
            project.AddNamespace("@");

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
                var moduleClass = new ClassModule(project);
                foreach (var route in module.Routes)
                {
                    //URL exists?
                    if (route.RelativeURL == null)
                    {
                        Logger.Write("[ERR] - No URL provided for route in sectoin '" + module.Name + "'.", 1);
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
                            //Figure out a data type from the API endpoint.


                            //Add the method.
                            moduleClass.Methods.Add(new GETMethodModule()
                            {
                                Async = route.Async,
                                Route = route.RelativeURL,
                                XMLSummary = route.GetSummary(),
                                Name = route.GetMethodName()
                            });
                            break;
                        case "POST":
                        case "post":
                            //Add the method.
                            moduleClass.Methods.Add(new POSTMethodModule()
                            {
                                Async = route.Async,
                                Route = route.RelativeURL,
                                XMLSummary = route.GetSummary(),
                                DataType = route.SentDataName,
                                Name = route.GetMethodName()
                            });
                            break;

                    }
                }

                //Add the class to root namespace.
                project.Namespaces["@"].Add(moduleClass);
            }

            return -1;
        }

        /// <summary>
        /// Sets the logger used by Burrito to the provided method.
        /// </summary>
        public void SetLogger(Action<string> logger)
        {
            Logger.Log = logger;
        }
    }
}
