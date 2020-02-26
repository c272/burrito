using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// Represents a single API schema in Burrito.
    /// </summary>
    public class APISchema
    {
        //The different sections of the API schema.
        [JsonProperty("sections")]
        public List<Section> Sections;

        //The name of the API schema.
        [JsonProperty("name")]
        public string Name;

        //The root path of the API.
        [JsonProperty("root")]
        public string RootPath;
    }

    /// <summary>
    /// Represents a single section within the API schema.
    /// </summary>
    public class Section
    {
        //The name of the section.
        [JsonProperty("name")]
        public string Name;

        //A list of routes for the section.
        [JsonProperty("routes")]
        public List<Route> Routes;
    }

    /// <summary>
    /// Represents a single route to generate within the API schema.
    /// </summary>
    public class Route
    {
        //The name for this route's method in the generated API.
        [JsonProperty("method")]
        public string MethodName;

        //The type of HTTP method that is being used, eg. GET, POST (required).
        [JsonProperty("type")]
        public string HTTPMethod;

        //Example data that is sent to/back from the URL.
        [JsonProperty("data")]
        public JObject ExampleData;

        //The URL for this route in the generated API (required).
        [JsonProperty("route")]
        public string RelativeURL;

        //A valid URL for this route with variables filled out.
        [JsonProperty("validroute")]
        public string ValidURL;

        //The name for the class derived from the returned data. (required).
        [JsonProperty("returns")]
        public string ReturnedDataName;

        //The name for the class derived from the sent data (required).
        [JsonProperty("sends")]
        public string SentDataName;

        //Whether an asynchronous method should be generated or not.
        [JsonProperty("async")]
        public bool Async;

        //A short description of what this route is used for.
        [JsonProperty("desc")]
        public string Description;

        /// <summary>
        /// Returns a valid XML summary for this route.
        /// </summary>
        public string GetSummary()
        {
            if (Description != null) { return Description; }
            return HTTPMethod + "s /" + RelativeURL + "/.";
        }

        /// <summary>
        /// Gets the variables from the relative route.
        /// </summary>
        public List<string> GetRouteVariables()
        {
            //Variables are defined using "{varname}".
            var matches = Regex.Matches(RelativeURL, "{[A-Za-z0-9_]+}");
            List<string> vars = new List<string>();
            foreach (Match match in matches)
            {
                vars.Add(match.Value.Substring(1, match.Length - 2));
            }

            return vars;
        }

        /// <summary>
        /// Returns a valid method name for this route.
        /// </summary>
        public string GetMethodName()
        {
            if (MethodName != null) 
            { 
                if (Async)
                    return MethodName + "Async";
                return MethodName;
            }

            //Root method is just called "root".
            if (RelativeURL == "")
            {
                return "GetRoot";
            }

            //Remove any variable parts in relative.
            string cleanedURL = Regex.Replace(RelativeURL, "{[A-Za-z0-9_]+}", "");

            //Split the relative route up by /.
            List<string> parts = cleanedURL.Split('/').ToList();

            string name = HTTPMethod.ToUpper().First().ToString() + HTTPMethod.ToLower().Substring(1);
            foreach (var part in parts)
            {
                //Remove invalid query bits, build name.
                int queryStartIndex = part.IndexOf('?');
                string validPart = queryStartIndex == -1 ? part : part.Substring(0, queryStartIndex);
                if (validPart.Length > 0)
                {
                    name += validPart.First().ToString().ToUpper() + validPart.Substring(1);
                }
            }

            //Add async if required.
            if (Async)
            {
                name += "Async";
            }

            return name;
        }

        /// <summary>
        /// Checks whether this route has valid properties or not.
        /// </summary>
        public bool Validate()
        {
            //URL exists?
            if (RelativeURL == null)
            {
                Logger.Write("[ERR] - No URL provided for route.", 1);
                return false;
            }

            //Is the route relative URL even valid?
            if (!Regex.IsMatch(RelativeURL, "^[A-Za-z_\\-\\.0-9\\?\\=%#@/\\{\\}]+$"))
            {
                Logger.Write("[ERR] - Invalid relative route provided ('" + RelativeURL + "'.", 1);
                return false;
            }

            //If the route contains variables, then check if there's a validURL.
            var routeVars = GetRouteVariables();
            if (routeVars.Count > 0 && ValidURL == null)
            {
                Logger.Write("[ERR] - Must include a valid, full relative URL to use when putting variables in routes.", 1);
                return false;
            }

            if (routeVars.Distinct().Count() != routeVars.Count)
            {
                Logger.Write("[ERR] - Route variables must be unique, duplicated names detected.", 1);
                return false;
            }

            //Return type valid?
            if (ReturnedDataName == null)
            {
                Logger.Write("[ERR] - No name given for data returned from route '" + RelativeURL + "'.", 1);
                return false;
            }
            if (!Regex.IsMatch(ReturnedDataName, "^[A-Za-z_0-9]+$"))
            {
                Logger.Write("[ERR] - Invalid returned data name, must be a valid C# field name.", 1);
                return false;
            }

            //Method type valid?
            if (HTTPMethod == null)
            {
                Logger.Write("[ERR] - No method type provided for route '" + RelativeURL + "'.", 1);
                return false;
            }

            //Send type valid (if necessary)?
            if (HTTPMethod.ToUpper() == "POST")
            {
                //Validate all POST properties are here.
                if (SentDataName == null)
                {
                    Logger.Write("[ERR] - No send data type name defined for POST route '" + RelativeURL + "'", 1);
                    return false;
                }
                if (!Regex.IsMatch(SentDataName, "^[A-Za-z_0-9]+$"))
                {
                    Logger.Write("[ERR] - Send data type name invalid, must be a valid C# field name.", 1);
                    return false;
                }
                if (ExampleData == null)
                {
                    Logger.Write("[ERR] - No example POST data given for route '" + RelativeURL + "'.", 1);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the true relative URL of the route without variables.
        /// </summary>
        public string GetRealURL()
        {
            if (GetRouteVariables().Count > 0)
            {
                return ValidURL;
            }

            return RelativeURL;
        }
    }
}
