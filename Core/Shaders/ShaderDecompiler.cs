using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Decomp.Core.Shaders
{
    public static class ShaderDecompiler
    {
        /// <summary>
        /// Decompiles a shader file to a human-readable text format.
        /// Supports .vsh and .psh (DirectX plain-text assembly) and
        /// .fxc (DirectX binary bytecode) used by the Taleworlds classic engine.
        /// </summary>
        public static void Decompile(string inputFile, string outputFile, string? gameVersion = null)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"Shader file not found: {inputFile}");
            }

            string extension = Path.GetExtension(inputFile).ToLowerInvariant();

            // .vsh (vertex shader) and .psh (pixel shader) are plain-text HLSL assembly
            // files used by the Mount & Blade engine. They are already human-readable
            // and only need to be read and written with the standard shader header.
            if (extension is ".vsh" or ".psh")
            {
                DecompileTextShader(inputFile, outputFile);
                return;
            }

            if (extension.Equals(".fxc", StringComparison.OrdinalIgnoreCase))
            {
                DecompileFxc(inputFile, outputFile);
                return;
            }

            throw new NotSupportedException(
                $"Shader decompilation for file type '{extension}' is not supported. " +
                "Supported formats: .vsh/.psh (DirectX text assembly), .fxc (DirectX binary bytecode).");
        }

        /// <summary>
        /// Reads a plain-text HLSL assembly shader (.vsh or .psh) and writes it
        /// to the output file with the standard shader header prepended.
        /// </summary>
        private static void DecompileTextShader(string inputFile, string outputFile)
        {
            EnsureOutputDirectory(outputFile);
            string shaderText = File.ReadAllText(inputFile, Encoding.UTF8);
            File.WriteAllText(outputFile, Header.Shaders + shaderText);
        }

        private static void DecompileFxc(string inputFile, string outputFile)
        {
            if (Shaders.IsWindowsPlatform)
            {
                // Try D3DDisassemble first; fall back to dxbc-disassembler if the DLL
                // is not available or the call fails.
                try
                {
                    DecompileFxcWithD3DDisassemble(inputFile, outputFile);
                    return;
                }
                catch (DllNotFoundException)
                {
                    // DLL not found — fall through to the cross-platform disassembler.
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("d3dcompiler_47.dll"))
                {
                    // DLL not found — fall through to the cross-platform disassembler.
                }
            }

            DecompileFxcWithDxbcDisassembler(inputFile, outputFile);
        }

        private static void DecompileFxcWithD3DDisassemble(string inputFile, string outputFile)
        {
            try
            {
                byte[] shaderBytecode = File.ReadAllBytes(inputFile);
#pragma warning disable CA1416
                string disassembledCode = Shaders.DisassembleFxcWithD3DDisassemble(shaderBytecode);
#pragma warning restore CA1416
                EnsureOutputDirectory(outputFile);
                File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Failed to disassemble .fxc file: d3dcompiler_47.dll not found. " +
                    "Ensure DirectX runtime is installed.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to disassemble .fxc file using D3DDisassemble: {ex.Message}", ex);
            }
        }

        private static void DecompileFxcWithDxbcDisassembler(string inputFile, string outputFile)
        {
            string disassemblerPath = GetDirectXDisassemblerPath();

            EnsureOutputDirectory(outputFile);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = disassemblerPath,
                    Arguments = $"-disassemble \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            // Read both streams concurrently before WaitForExit to prevent deadlocks.
            string disassembledCode = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(10000))
            {
                process.Kill();
                throw new InvalidOperationException(
                    "dxbc-disassembler process timed out after 10 seconds.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to disassemble .fxc file. Exit code: {process.ExitCode}\n" +
                    $"Error output: {(string.IsNullOrEmpty(standardError) ? "(none)" : standardError)}");
            }

            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static void EnsureOutputDirectory(string outputFile)
        {
            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        private static string GetDirectXDisassemblerPath()
        {
            string[] absolutePaths =
            {
                Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler.exe"),
                Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler"),
            };

            foreach (string path in absolutePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "dxbc-disassembler.exe"
                : "dxbc-disassembler";
        }
    }
}
