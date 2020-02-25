using System.Collections.Generic;

namespace Burrito
{
    /// <summary>
    /// Represents an entire project's worth of code.
    /// </summary>
    public class ProjectCode
    {
        public Dictionary<string, string> Files = new Dictionary<string, string>();
        public string ProjectName;

        public ProjectCode(string name)
        {
            ProjectName = name;

            //Add the default API file.
            //...
        }
    }
}