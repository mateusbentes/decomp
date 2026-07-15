using System;

namespace Decomp.Core
{
    public static class Decompiler
    {
        public static event Action<string>? LogMessage;

        public static void Decompile(string inputPath, string outputPath, string version = "VanillaWarband")
        {
            LogMessage?.Invoke($"Decompiling {inputPath} to {outputPath} using {version} version");
        }

        public static void RaiseLogMessage(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}
