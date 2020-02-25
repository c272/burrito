using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burrito
{
    class DataTypeCreator
    {
        /// <summary>
        /// Derives an API data type from a given HTTP GET/POST route.
        /// </summary>
        public static ClassModule DeriveFromRoute(string baseURI, Route route, JObject postData=null)
        {
            //Attempt to ping the route for a response.
            string response;
            if (postData == null)
            {
                //GET route.
                try
                {
                    response = HTTP.Get(baseURI + route.RelativeURL);
                }
                catch (Exception e)
                {
                    Logger.Write("[ERR] - Failed to derive data from route '" + route.RelativeURL + "' could not GET: " + e.Message, 1);
                    return null;
                }
            }
            else
            {
                //POST route.
                try
                {
                    response = HTTP.Post(baseURI + route.RelativeURL, postData.ToString());
                }
                catch (Exception e)
                {
                    Logger.Write("[ERR] - Failed to derive data from route '" + route.RelativeURL + "', POST failed: " + e.Message, 1);
                    return null;
                }
            }

            //Response obtained, translate it to a JObject.
            JObject jobj;
            try
            {
                jobj = JObject.Parse(response);
            }
            catch
            {
                Logger.Write("[ERR] - Failed to derive data from route '" + route.RelativeURL + "', invalid JSON response.", 1);
                return null;
            }

            //Construct a class from it.
            return FromJObject(jobj, route.ReturnedDataName);
        }

        /// <summary>
        /// Derives a C# class module from a JObject.
        /// </summary>
        public static ClassModule FromJObject(JObject jobj, string name)
        {
            //Create ClassModule.
            var module = new ClassModule(name);

            //If the name or JObject is null, invalid.
            if (jobj == null)
            {
                Logger.Write("[ERR] - Invalid JSON data given to create a class module from, no data.", 1);
                return null;
            }
            if (name == null)
            {
                Logger.Write("[ERR] - No name provided for creating returned data from a route.", 1);
                return null;
            }

            //Create the class module.
            foreach (var prop in jobj.Properties())
            {
                module.Fields.Add(GenerateField(name, prop.Name, prop.Value));
            }

            return module;
        }

        private static Field GenerateField(string rootName, string name, JToken value)
        {
            switch (value.Type)
            {
                //array
                case JTokenType.Array:
                    //How long is the array?
                    var arr = (JArray)value;
                    if (arr.Count == 0)
                    {
                        Logger.Log("[WARN] - Cannot generate a type from an empty array. Leaving an empty list type here.");
                        return new Field(name, ClassModule.Empty());
                    }

                    //generate field
                    var subObjField = GenerateField(rootName, name, arr[0]);
                    subObjField.IsList = true;

                    //return field
                    return subObjField;

                //object (new class)
                case JTokenType.Object:
                    //generate type.
                    var subObjType = FromJObject((JObject)value, rootName + "_" + name);
                    while (BurritoAPI.Project.Namespaces["Data"].FindIndex(x => x.Name == subObjType.Name) != -1)
                    {
                        subObjType.Name += "_";
                    }
                    BurritoAPI.Project.Namespaces["Data"].Add(subObjType);

                    //add field.
                    return new Field(name, subObjType.Name);

                //raw values
                case JTokenType.Boolean:
                    return new Field(name, "bool");
                case JTokenType.Bytes:
                    return new Field(name, "byte[]");
                case JTokenType.Date:
                    return new Field(name, "DateTime");
                case JTokenType.Float:
                    return new Field(name, "float");
                case JTokenType.Guid:
                    return new Field(name, "Guid");
                case JTokenType.Integer:
                    return new Field(name, "int");
                case JTokenType.String:
                    return new Field(name, "string");

                default:
                    Logger.Write("[WARN]- Unsupported type in JSON to create a class from: '" + value.Type.ToString() + "' for generated data class '" + name + "'. Skipped property.", 1);
                    return null;
            }
        }
    }
}
