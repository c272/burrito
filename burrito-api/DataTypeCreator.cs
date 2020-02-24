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
        }
    }
}
