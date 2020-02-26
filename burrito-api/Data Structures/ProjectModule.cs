using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// Represents an entire API C# class library in an abstract manner.
    /// </summary>
    public class ProjectModule
    {
        //The namespaces within this project, and their classes.
        public Dictionary<string, List<ClassModule>> Namespaces { get; private set; } = new Dictionary<string, List<ClassModule>>();
        
        //A list of public, static globals and their type names.
        public Dictionary<string, string> StaticGlobals { get; set; } = new Dictionary<string, string>();

        //The root URL for this project.
        public string RootURL;

        //The name of this project.
        public string Name;

        public ProjectModule(string name, string rootURI)
        {
            Name = name;
            RootURL = rootURI;
        }

        /// <summary>
        /// Adds a namespace to this project. "@" represents the root namespace.
        /// </summary>
        public void AddNamespace(string name)
        {
            if (Namespaces.ContainsKey(name))
            {
                throw new Exception("A namespace already exists with this name.");
            }

            Namespaces.Add(name, new List<ClassModule>());
        }

        /// <summary>
        /// Generates the project code module for this 
        /// </summary>
        public ProjectCode GenerateCode()
        {
            var code = new ProjectCode(Name);

            //Rename any class called "_globals" with an extra underscore.
            if (Namespaces["@"].FindIndex(x => x.Name == "_globals") != -1)
            {
                Namespaces["@"].First(x => x.Name == "_globals").Name += "_";
            }

            //Add the root address as a class called "_globals".
            var globalModule = new ClassModule("_globals");
            globalModule.StaticFields.Add(new Static("RootURL", "string")
            {
                Value = "@\"" + RootURL + "\""
            });
            Namespaces["@"].Add(globalModule);


            //Loop over namespaces for generation.
            foreach (var ns in Namespaces)
            {
                //Correct any collisions in this namespace.
                foreach (var module in ns.Value)
                {
                    while (ns.Value.FindAll(x => x.Name == module.Name).Count > 1)
                    {
                        module.Name += "_";
                    }
                }

                //Generate code for the namespace.
                string thisNamespaceFull = (ns.Key == "@") ? Name : Name + "." + ns.Key;
                foreach (var module in ns.Value)
                {
                    code.Files.Add(new ProjectFileInfo() {
                        ClassName = module.Name,
                        Namespace = ns.Key
                    }, module.GenerateCode(thisNamespaceFull, Name + ".Data"));
                }
            }

            return code;
        }
    }
}
