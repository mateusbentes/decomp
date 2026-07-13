using System;
using System.Collections.Generic;
using System.Linq;

namespace Decomp.Core.Operators
{
    public class Operator
    {
        public string Name { get; set; }
        public int OpCode { get; set; }
        public List<string> Parameters { get; set; }

        public Operator(string name, int opCode, List<string> parameters)
        {
            Name = name;
            OpCode = opCode;
            Parameters = parameters;
        }

        public string GetParameter(int index, string s)
        {
            if (index < Parameters.Count)
            {
                return Parameters[index];
            }
            return s;
        }

        public static IEnumerable<Operator> GetCollection(string gameVersion)
        {
            switch (gameVersion)
            {
                case "VanillaWarband":
                    return VanillaWarband.GetOperators();
                case "VanillaClassic":
                    return VanillaClassic.GetOperators();
                case "WSE320":
                    return WarbandScriptEnhancer320.GetOperators();
                case "WSE450":
                    return WarbandScriptEnhancer450.GetOperators();
                case "Caribbean":
                    return Caribbean.GetOperators();
                default:
                    throw new ArgumentException("Invalid game version specified.");
            }
        }
    }
}
