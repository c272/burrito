using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BurritoCore
{
    /// <summary>
    /// Delegate for an API call return.
    /// </summary>
    public delegate void APICallReturnDelegate<T>(T result);

    /// <summary>
    /// Wrapper class for calling any given API and converting the result to a C# class.
    /// Josh rewrite this pls - Larry
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Gets a specific API endpoint synchronously and returns it as a specific class.
        /// </summary>
        public static T Get<T>(string webAddr)
        {
            //Create a request to the server.
            var request = (HttpWebRequest)WebRequest.Create(webAddr);
            request.Method = "GET";

            //Get a response.
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            //Cast it to the given class.
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        /// <summary>
        /// Gets a specific API endpoint synchronously and returns it as a specific class.
        /// </summary>
        public static T Post<T>(string webAddr, string data)
        {
            //Create a request to the server.
            var request = (HttpWebRequest)WebRequest.Create(webAddr);
            request.Method = "POST";
            request.ContentType = "application/json";
            var bytes = Encoding.Unicode.GetBytes(data);
            request.ContentLength = bytes.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, data.Length);
            }

            //Get a response.
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            //Cast it to the given class.
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        /// <summary>
        /// Gets a specific API endpoint asynchronously, and returns it as a specific class.
        /// </summary>
        public static Task GetAsync<T>(string webAddr, APICallReturnDelegate<T> doneFunc)
        {
            //Run as an async task.
            var task = Task.Run(() => 
            { 
                var result = Get<T>(webAddr);
                doneFunc(result);
            });

            return task;
        }

        /// <summary>
        /// Gets a specific API endpoint asynchronously, and returns it as a specific class.
        /// </summary>
        public static Task PostAsync<T>(string webAddr, string data, APICallReturnDelegate<T> doneFunc)
        {
            //Run as an async task.
            var task = Task.Run(() =>
            {
                var result = Post<T>(webAddr, data);
                doneFunc(result);
            });

            return task;
        }
    }
}