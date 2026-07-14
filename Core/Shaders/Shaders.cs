using System;
using System.Runtime.InteropServices;

namespace Decomp.Core.Shaders
{
    public static class Shaders
    {
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [DllImport("d3dcompiler_47.dll", EntryPoint = "D3DDisassemble", CharSet = CharSet.Unicode)]
        private static extern int D3DDisassemble(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pSrcData,
            int SrcDataSize,
            uint Flags,
            string? szComments,
            out IDisposable ppDisassembly);

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

        internal static string DisassembleFxcWithD3DDisassemble(byte[] shaderBytecode)
        {
            if (!IsWindowsPlatform)
            {
                throw new PlatformNotSupportedException(
                    "D3DDisassemble is only available on Windows.");
            }

            try
            {
                int result = D3DDisassemble(
                    shaderBytecode,
                    shaderBytecode.Length,
                    0,
                    null,
                    out IDisposable disassembly);

                if (result < 0)
                {
                    throw new InvalidOperationException(
                        $"D3DDisassemble failed with HRESULT: 0x{result:X8}");
                }

                // O tipo real é ID3DBlob, mas usamos dynamic para evitar dependências adicionais.
                dynamic blob = disassembly;
                IntPtr bufferPointer = blob.GetBufferPointer();
                int bufferSize = blob.GetBufferSize();
                string disassembledCode = Marshal.PtrToStringAnsi(bufferPointer, bufferSize);
                blob.Dispose();

                return disassembledCode ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to disassemble shader using D3DDisassemble.", ex);
            }
        }
    }
}
