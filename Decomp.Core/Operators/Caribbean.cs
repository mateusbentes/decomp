using System.Collections.Generic;

namespace Decomp.Core.Operators
{
    public static class Caribbean
    {
        public static IEnumerable<Operator> GetOperators()
        {
            // Caribbean-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more Caribbean-specific operators here
        }
    }
}
