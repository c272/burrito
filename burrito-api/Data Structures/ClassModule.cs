using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// Represents a single class to be written into a C# file.
    /// </summary>
    public class ClassModule
    {
        //The parent project of this class module.
        public ProjectModule Project;

        //The XML comment summary to use above the class name.
        public string XMLSummary;

        //The name of the class.
        public string Name;

        //Namespaces to include above the class.
        public List<string> Includes = new List<string>()
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks"
        };

        //The list of methods to implement within the class.
        public List<APIMethodModule> Methods = new List<APIMethodModule>();

        //A list of public fields to create within this class.
        public List<Field> Fields = new List<Field>();

        public ClassModule(ProjectModule proj, string name)
        {
            Project = proj;
            Name = name;
        }
    }

    /// <summary>
    /// Represents a single public field within a class module.
    /// </summary>
    public class Field
    {
        public string TypeName;
        public string Name;
    }
}
