using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace burritocli
{
    /// <summary>
    /// Parses arguments passed into Burrito.
    /// </summary>
    public class ArgumentParser
    {
        public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();
        public List<string> Flags { get; private set; } = new List<string>();

        public List<string> validArgs = new List<string>()
        {
            "p","s","c","dll","debug","nnc","aas"
        };

        //Parses the given arguments.
        public ArgumentParser(string[] args)
        {
            for (int i=0; i<args.Length; i++)
            {
                if (!validArgs.Contains(args[i]))
                {
                    Logger.Exit("Invalid argument supplied '" + args[i] + "'.");
                    return;
                }

                //Is it a value?
                if (i != args.Length - 1 && !args[i+1].StartsWith("-"))
                {
                    //Already exists?
                    if (Values.ContainsKey(args[i]))
                    {
                        //Error.
                        Logger.Exit("Error parsing value argument '" + args[i] + "', a value already exists with that name.");
                        return;
                    }

                    //Add to values.
                    Values.Add(args[i].Substring(1), args[i + 1]);
                    i++;
                    continue;
                }

                //Flag?
                if (args[i].StartsWith("-"))
                {
                    Flags.Add(args[i].Substring(1));
                    continue;
                }

                //Invalid value.
                Logger.Exit("Invalid argument (argument " + (i + 1) + "), not a flag or a value.");
                return;
            }    
        }

        /// <summary>
        /// Returns whether a flag has been set or not.
        /// </summary>
        public bool GetFlag(string name)
        {
            return Flags.Contains(name);
        }

        /// <summary>
        /// Returns the value of a named parameter, or null if not set.
        /// </summary>
        public string GetValue(string name)
        {
            if (!Values.ContainsKey(name))
            {
                return null;
            }

            return Values[name];
        }
    }
}
