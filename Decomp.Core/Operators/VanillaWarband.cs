using System.Collections.Generic;

namespace Decomp.Core.Operators
{
    public static class VanillaWarband
    {
        public static IEnumerable<Operator> GetOperators()
        {
            // Warband-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more Warband-specific operators here
        }
    }
}
