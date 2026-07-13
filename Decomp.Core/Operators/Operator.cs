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
                    return GetWarbandOperators();
                case "VanillaClassic":
                    return GetClassicOperators();
                default:
                    throw new ArgumentException("Invalid game version specified.");
            }
        }

        private static IEnumerable<Operator> GetWarbandOperators()
        {
            // Warband-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more Warband-specific operators here
        }

        private static IEnumerable<Operator> GetClassicOperators()
        {
            // Classic 1.011-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more Classic 1.011-specific operators here
        }
    }
}
