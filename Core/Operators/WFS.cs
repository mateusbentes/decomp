using System.Collections.Generic;

namespace Decomp.Core.Operators
{
    /// <summary>
    /// Mount &amp; Blade: With Fire &amp; Sword operator set.
    /// WFS is built on the same engine as Warband 1.153 and shares its opcode table.
    /// </summary>
    public class WFSVersion : IGameVersion
    {
        public IEnumerable<Operator> GetOperators() => new Warband1153Version().GetOperators();
    }
}
