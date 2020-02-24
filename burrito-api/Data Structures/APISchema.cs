using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //The type of HTTP method that is being used, eg. GET, POST.
        [JsonProperty("type")]
        public string HTTPMethod;

        //Example data that is sent to/back from the URL.
        [JsonProperty("data")]
        public JObject ExampleData;

        //The URL for this route in the generated API (required).
        [JsonProperty("route")]
        public string RelativeURL;

        //The name for the class derived from the returned data. (required).
        [JsonProperty("returns")]
        public string ReturnedDataName;

        //The name for the class derived from the sent data.
        [JsonProperty("sent")]
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
        /// Returns a valid method name for this route.
        /// </summary>
        public string GetMethodName()
        {
            if (MethodName != null) { return MethodName; }

            //Root method is just called "root".
            if (RelativeURL == "")
            {
                return "GetRoot";
            }

            //Split the relative route up by /.
            List<string> parts = RelativeURL.Split('/').ToList();
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

            return name;
        }
    }
}
