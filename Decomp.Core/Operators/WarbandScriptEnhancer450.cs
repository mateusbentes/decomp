using System.Collections.Generic;

namespace Decomp.Core.Operators
{
    public static class WarbandScriptEnhancer450
    {
        public static IEnumerable<Operator> GetOperators()
        {
            // WSE v4.7.7-specific operators
            yield return new Operator("assign", 0, new List<string> { "var", "val" });
            yield return new Operator("add", 1, new List<string> { "var", "val" });
            // Add more WSE v4.7.7-specific operators here
        }
    }
}
