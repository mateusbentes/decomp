using System;

namespace Decomp.Core
{
    public static class Decompiler
    {
        public static event Action<string>? LogMessage;

        public static void Decompile(string inputPath, string outputPath, string version)
        {
            // Implementação real da descompilação viria aqui
            LogMessage?.Invoke($"Decompiling {inputPath} to {outputPath} using {version} version");
        }

        internal static void RaiseLogMessage(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}
