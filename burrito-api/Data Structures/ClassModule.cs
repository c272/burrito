using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
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
            "System.Threading.Tasks",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq"
        };

        //The list of methods to implement within the class.
        public List<APIMethodModule> Methods = new List<APIMethodModule>();

        //A list of public fields to create within this class.
        public List<Field> Fields = new List<Field>();

        //A list of public static fields.
        public List<Static> StaticFields = new List<Static>();

        public ClassModule(string name)
        {
            Name = name;
        }

        //Gets the name of the "empty" type.
        public static string Empty()
        {
            return "_empty";
        }

        /// <summary>
        /// Generates the class code for this module.
        /// </summary>
        public string GenerateCode(string ns, params string[] extraIncludes)
        {
            //Generate the includes.
            string code = "";
            foreach (var include in Includes)
            {
                code += "using " + include + ";\n";
            }
            foreach (var include in extraIncludes)
            {
                code += "using " + include + ";\n";
            }
            code += "\n";

            //Namespace and class header.
            code += "namespace " + ns + "\n{\n";
            code += "//<summary>\n///The " + Name + " class, rolled up by Burrito.\n///</summary>\n";
            code += "public class " + Name + "\n{\n";

            //Generate every single static field.
            foreach (var staticField in StaticFields)
            {
                code += "public static " + staticField.TypeName + " " + staticField.Name + " = " + staticField.Value + ";\n";
            }

            code += "\n";

            //Generate every single field.
            foreach (var field in Fields)
            {
                string fieldName = field.Name;
                if (BurritoAPI.FollowNamingConventions)
                {
                    fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                    code += "[JsonProperty(\"" + field.Name + "\")]";
                }

                if (field.IsList)
                {
                    code += "public List<" + field.TypeName + "> " + fieldName + ";\n";
                }
                else
                {
                    code += "public " + field.TypeName + " " + fieldName + ";\n";
                }
            }

            code += "\n";

            //Generate every single method.
            GenerateMethods(ref code, Methods);

            //Close class and namespace, done.
            code += "}\n}";

            //Normalize whitespace and prettify using roslyn.
            try
            {
                code = CSharpSyntaxTree.ParseText(code).GetRoot().NormalizeWhitespace().ToFullString();
            }
            catch (Exception e)
            {
                Logger.Write("[WARN] - Failed to beautify code, could not parse as C#: '" + e.Message + "'.", 2);
            }
            return code;
        }

        /// <summary>
        /// Generates method code for the supplied list of methods.
        /// </summary>
        private void GenerateMethods(ref string code, List<APIMethodModule> methods)
        {
            //Generate every single method.
            foreach (var method in methods)
            {
                //If generate async and sync is on, and async, generate the synchronous method first.
                if (method.Async && BurritoAPI.GenerateAsyncAndSync)
                {
                    var methodCopy = method.Clone();
                    methodCopy.Async = false;
                    GenerateMethods(ref code, new List<APIMethodModule>() { methodCopy });
                }

                //XML summary.
                code += "\n";
                code += "///<summary>\n///" + method.XMLSummary.Replace("\n", "") + "\n///</summary>\n";

                //Method header.
                if (method.Async)
                {
                    //awaitable return type.
                    code += "public static Task ";
                }
                else
                {
                    code += "public static " + method.GetReturnType() + " ";
                }

                //Open parameters.
                code += method.Name + "(";

                //Build a list of parameters.
                List<string> params_ = new List<string>();
                if (method is POSTMethodModule)
                {
                    var postMethod = (POSTMethodModule)method;
                    params_.Add(postMethod.SentDataType.Name + " postData");
                }
                if (method.Async)
                {
                    params_.Add("BurritoCore.APICallReturnDelegate<" + method.GetReturnType() + "> callback");
                }

                //Optional template params Templates are replaced by blank by default.
                foreach (var param in method.RouteParams)
                {
                    //parameters in relative route.
                    params_.Add("string " + param + "=\"\"");
                }

                //Add params, close.
                foreach (var param in params_)
                {
                    code += param + ",";
                }
                code = code.TrimEnd(',');
                code += ")\n{\n";

                //What type of method to use?
                string methodName = "";
                if (method is POSTMethodModule) { methodName = "Post"; }
                else if (method is GETMethodModule) { methodName = "Get"; }
                else
                {
                    Logger.Write("[ERR] - Fatal error generating code, invalid method module type '" + method.GetType().ToString() + "'.", 1);
                    Environment.Exit(0);
                }

                //Write out the return statement.
                code += "return BurritoCore.API." + methodName;
                if (method.Async)
                {
                    code += "Async";
                }
                code += "<" + method.GetReturnType() + ">(";

                //Required URL parameter, post data and async.
                code += "_globals.RootURL + $\"" + method.Route + "\", ";
                if (method is POSTMethodModule)
                {
                    code += "JsonConvert.SerializeObject(postData), ";
                }
                if (method.Async)
                {
                    code += "callback";
                }
                code = code.TrimEnd(',', ' ');

                //Close the parameters and method.
                code += ");\n}\n";
            }
        }
    }

    /// <summary>
    /// Represents a single public field within a class module.
    /// </summary>
    public class Field
    {
        public string TypeName;
        public string Name;
        public bool IsList = false;

        public Field(string name, string type)
        {
            TypeName = type;
            Name = name;
        }
    }

    /// <summary>
    /// Represents a single static field within a class module.
    /// </summary>
    public class Static : Field
    {
        //The string value of the field.
        public string Value;

        public Static(string name, string type) : base(name, type) { }
    }
}
