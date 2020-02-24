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
    }

    /// <summary>
    /// A single POST API method.
    /// </summary>
    public class POSTMethodModule : APIMethodModule
    {
        //The data type to POST.
        public string DataType;
    }

    /// <summary>
    /// A single GET API method.
    /// </summary>
    public class GETMethodModule : APIMethodModule { }
}