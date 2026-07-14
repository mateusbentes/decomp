using System;
using System.Runtime.InteropServices;

namespace Decomp.Core.Shaders
{
    public static class Shaders
    {
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [ComImport]
        [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ID3DBlob
        {
            [PreserveSig]
            IntPtr GetBufferPointer();

            [PreserveSig]
            int GetBufferSize();
        }

        [DllImport("d3dcompiler_47.dll", EntryPoint = "D3DDisassemble", CharSet = CharSet.Unicode)]
        private static extern int D3DDisassemble(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pSrcData,
            int SrcDataSize,
            uint Flags,
            string? szComments,
            out ID3DBlob ppDisassembly);

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
                    out ID3DBlob blob);

                if (result < 0)
                {
                    throw new InvalidOperationException(
                        $"D3DDisassemble failed with HRESULT: 0x{result:X8}");
                }

                IntPtr bufferPointer = blob.GetBufferPointer();
                int bufferSize = blob.GetBufferSize();
                string disassembledCode = Marshal.PtrToStringAnsi(bufferPointer, bufferSize);
                return disassembledCode ?? string.Empty;
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "d3dcompiler_47.dll not found. Ensure DirectX runtime is installed.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to disassemble shader using D3DDisassemble.", ex);
            }
        }
    }
}
