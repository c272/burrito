using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burrito
{
    /// <summary>
    /// All string extensions for the Burrito API.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// An exhaustive list of all reserved words in C#.
        /// </summary>
        public static string[] RESERVED_WORDS = new string[]
        {
            "abstract",	"as",	"base",	"bool",	
            "break",	"byte",	"case",	"catch",	
            "char",	"checked",	"class",   "const",
            "continue",	"decimal",	"default",	"delegate",
            "do",	"double",  "else",	"enum",
            "event",   "explicit",    "extern",	"false",
            "finally",	"fixed", "float",   "for",	
            "foreach",	"goto",	"if",	"implicit",
            "in", "int", "interface",   "internal",	
            "is",	"lock",	"long",    "namespace",
            "new",	"null",	"object", "operator",
            "out",	"override",	"params",  "private",
            "protected", "public", "readonly", "ref",
            "return",	"sbyte", "sealed", "short",	
            "sizeof",	"stackalloc",	"static", "string",
            "struct",  "switch",	"this",    "throw",	
            "true",	"try",	"typeof",	"uint",
            "ulong",   "unchecked",	"unsafe", "ushort",
            "using",	"virtual", "void",
            "volatile",	"while"		
        };

        /// <summary>
        /// Checks if a given word is a C# reserved word or not.
        /// </summary>
        public static bool IsReservedWord(this string word)
        {
            return RESERVED_WORDS.Contains(word);
        }
    }
}
