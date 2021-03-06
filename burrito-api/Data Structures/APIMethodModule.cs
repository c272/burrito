﻿using System;
using System.Collections.Generic;

namespace Burrito
{
    /// <summary>
    /// Represents a single API method within a class.
    /// </summary>
    public abstract class APIMethodModule
    {
        //The XML comment summary for this method.
        public string XMLSummary;
        
        //Whether this method is asynchronous or not.
        public bool Async;

        //The route for this method to call.
        public string Route;

        //The name of this method.
        public string Name;

        //Name of the data type that is received back.
        public ClassModule ReceivedDataType;

        //List of variables in the route that need to be put as params.
        public List<string> RouteParams { get; set; } = new List<string>();

        //Whether this method returns a list or not.
        public bool ReturnsList { get; set; }

        /// <summary>
        /// Clones this API method module.
        /// </summary>
        public APIMethodModule Clone()
        {
            return (APIMethodModule)this.MemberwiseClone();
        }

        /// <summary>
        /// Gets a string representation of this method's return type.
        /// </summary>
        public string GetReturnType()
        {
            if (ReturnsList)
            {
                return "List<" + ReceivedDataType.Name + ">";
            }
            else
            {
                return ReceivedDataType.Name;
            }
        }
    }

    /// <summary>
    /// A single POST API method.
    /// </summary>
    public class POSTMethodModule : APIMethodModule
    {
        //The data type to POST.
        public ClassModule SentDataType;
    }

    /// <summary>
    /// A single GET API method.
    /// </summary>
    public class GETMethodModule : APIMethodModule
    {
    }
}