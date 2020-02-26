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
        public static ClassModule DeriveFromRoute(string baseURI, Route route, ref bool isList, JObject postData=null)
        {
            //Attempt to ping the route for a response.
            string response;
            if (postData == null)
            {
                //GET route.
                try
                {
                    response = HTTP.Get(baseURI + route.GetRealURL());
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
                    response = HTTP.Post(baseURI + route.GetRealURL(), postData.ToString());
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
                //Is it an object?
                jobj = JObject.Parse(response);
                isList = false;

                //Construct a class from it.
                return FromJObject(jobj, route.ReturnedDataName);
            }
            catch
            {
                //Maybe it's an array?
                try
                {
                    var jarr = JArray.Parse(response);

                    //Yes, set method return type as array.
                    isList = true;
                    
                    //An array of what?
                    if (jarr.Count == 0)
                    {
                        Logger.Write("[WARN] - Cannot derive type from empty array, assuming as an empty. (route '" + route.RelativeURL + "')", 2);
                        return BurritoAPI.Project.Namespaces["@"].Find(x => x.Name == "_empty");
                    }

                    //Deriving type.
                    var field = GenerateField(route.ReturnedDataName, "", jarr[0]);

                    //Return a dummy class with the right typename.
                    return new ClassModule(field.TypeName);
                }
                catch
                {
                    Logger.Write("[ERR] - Failed to derive data from route '" + route.RelativeURL + "', invalid JSON response. (route '" + route.RelativeURL + "')", 1);
                    return null;
                }
            }
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
                Logger.Write("[ERR] - Invalid JSON data given to create a class module from, no data. ('" + name + "')", 1);
                return null;
            }
            if (name == null)
            {
                Logger.Write("[ERR] - No name provided for creating returned data from a route. ('" + name + "')", 1);
                return null;
            }

            //Create the class module.
            foreach (var prop in jobj.Properties())
            {
                module.Fields.Add(GenerateField(name, prop.Name, prop.Value));
            }

            return module;
        }

        /// <summary>
        /// Generates a field to use in a generated class.
        /// </summary>
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
                        Logger.Log("[WARN] - Cannot generate a type from an empty array. Leaving an empty list type here. ('" + name + "')");
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
                case JTokenType.Null:
                    Logger.Write("[WARN] - Null type in JSON at property '" + name + "', assumed as an object. ('" + name + "')", 2);
                    return new Field(name, "object ");

                default:
                    Logger.Write("[WARN] - Unsupported type in JSON to create a class from: '" + value.Type.ToString() + "' for generated data class '" + name + "'. Skipped property.", 2);
                    return null;
            }
        }
    }
}
