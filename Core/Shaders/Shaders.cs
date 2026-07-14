using System;
using System.Runtime.InteropServices;

namespace Decomp.Core.Shaders
{
    public static class Shaders
    {
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static void Decompile(string sFileName)
        {
            throw new PlatformNotSupportedException(
                "Direct3D shader decompilation is no longer supported in this class. " +
                "Use ShaderDecompiler.Decompile() for cross-platform shader decompilation.");
        }

        public static void DecompileFxc(string inputFile, string outputFile)
        {
            throw new PlatformNotSupportedException(
                "Direct3D .fxc decompilation is no longer supported in this class. " +
                "Use ShaderDecompiler.Decompile() for cross-platform shader decompilation.");
        }
    }
}
