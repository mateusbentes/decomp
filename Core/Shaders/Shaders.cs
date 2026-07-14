using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Decomp.Core.Shaders
{
    public static class Shaders
    {
        /// <summary>
        /// Checks if the current platform is Windows.
        /// </summary>
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [ComImport]
        [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ID3DBlob
        {
            /// <summary>
            /// Retrieves a pointer to the blob's data.
            /// </summary>
            [PreserveSig]
            IntPtr GetBufferPointer();

            /// <summary>
            /// Retrieves the size of the blob's data.
            /// </summary>
            [PreserveSig]
            int GetBufferSize();
        }

        /// <summary>
        /// Disassembles compiled HLSL code.
        /// </summary>
        [DllImport("d3dcompiler_47.dll", EntryPoint = "D3DDisassemble", CharSet = CharSet.Unicode)]
        private static extern int D3DDisassemble(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pSrcData,
            int SrcDataSize,
            uint Flags,
            string? szComments,
            out ID3DBlob ppDisassembly);

        /// <summary>
        /// Decompiles a shader file (placeholder method).
        /// </summary>
        /// <param name="sFileName">Input shader file path</param>
        /// <exception cref="PlatformNotSupportedException">Always thrown as this method is deprecated</exception>
        public static void Decompile(string sFileName)
        {
            throw new PlatformNotSupportedException(
                "Direct3D shader decompilation is no longer supported in this class. " +
                "Use ShaderDecompiler.Decompile() for cross-platform shader decompilation.");
        }

        /// <summary>
        /// Decompiles a compiled .fxc shader file to text format.
        /// </summary>
        /// <param name="inputFile">Path to the input .fxc file</param>
        /// <param name="outputFile">Path to save the disassembled output</param>
        /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows</exception>
        public static void DecompileFxc(string inputFile, string outputFile)
        {
            if (!IsWindowsPlatform)
            {
                throw new PlatformNotSupportedException(
                    "Direct3D .fxc decompilation is only supported on Windows.");
            }

            var shaderBytecode = File.ReadAllBytes(inputFile);
            var disassembledCode = DisassembleFxcWithD3DDisassemble(shaderBytecode);
            File.WriteAllText(outputFile, disassembledCode);
        }

        /// <summary>
        /// Internal method to disassemble shader bytecode using D3DDisassemble.
        /// </summary>
        /// <param name="shaderBytecode">Compiled shader bytecode</param>
        /// <returns>Disassembled shader code as string</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows</exception>
        [SupportedOSPlatform("windows")]
        internal static string DisassembleFxcWithD3DDisassemble(byte[] shaderBytecode)
        {
            if (!IsWindowsPlatform)
            {
                throw new PlatformNotSupportedException(
                    "D3DDisassemble is only available on Windows.");
            }

            ID3DBlob? blob = null;
            try
            {
                int result = D3DDisassemble(
                    shaderBytecode,
                    shaderBytecode.Length,
                    0,
                    null,
                    out blob);

                if (result < 0)
                {
                    throw new InvalidOperationException(
                        $"D3DDisassemble failed with HRESULT: 0x{result:X8}");
                }

                IntPtr bufferPointer = blob.GetBufferPointer();
                int bufferSize = blob.GetBufferSize();
                string disassembledCode = Marshal.PtrToStringAnsi(bufferPointer, bufferSize) ?? string.Empty;

                return disassembledCode;
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
            finally
            {
                if (blob != null)
                {
                    Marshal.FinalReleaseComObject(blob);
                }
            }
        }
    }
}
