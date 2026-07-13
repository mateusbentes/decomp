using System.Collections.Generic;

namespace Decomp.Core.Operators
{
    public static class VanillaClassic
    {
        public static IEnumerable<Operator> GetOperators()
        {
            // Classic 1.011-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more Classic 1.011-specific operators here
        }
    }
}
