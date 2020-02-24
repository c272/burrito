using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace burrito
{
    /// <summary>
    /// Information about this running instance of Burrito.
    /// </summary>
    public static class RuntimeInformation
    {
        //Whether to compile the library after generating it.
        public static bool CompileAfterGeneration { get; set; } = false;

        //What path to generate the library at.
        public static string GenerationPath { get; set; } = Environment.CurrentDirectory;

        //Where to draw the API schema from to use.
        public static string APISchemaPath { get; set; } = null;
    }
}
