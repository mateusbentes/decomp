using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Decomp.Core.Shaders
{
    public static class ShaderDecompiler
    {
        public static void Decompile(string inputFile, string outputFile, string? gameVersion = null)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"Shader file not found: {inputFile}");
            }

            if (string.IsNullOrEmpty(gameVersion))
            {
                gameVersion = "VanillaWarband";
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

            bool isFxcFile = extension.Equals(".fxc", StringComparison.OrdinalIgnoreCase);
            bool isGlslFile = extension.Equals(".glsl", StringComparison.OrdinalIgnoreCase);
            bool isWarbandShader = gameVersion.Equals("VanillaWarband", StringComparison.OrdinalIgnoreCase) ||
                                   gameVersion.Equals("WSE320", StringComparison.OrdinalIgnoreCase) ||
                                   gameVersion.Equals("WSE450", StringComparison.OrdinalIgnoreCase);

            if (isFxcFile)
            {
                DecompileFxc(inputFile, outputFile);
            }
            else if (isGlslFile || (isWarbandShader && IsOpenGLShader(inputFile)))
            {
                DecompileGlslWithSpirvCross(inputFile, outputFile);
            }
            else
            {
                throw new NotSupportedException(
                    $"Shader decompilation for file type '{extension}' is not supported. " +
                    "Supported formats: .vsh/.psh (DirectX text assembly), .fxc (DirectX binary), .glsl (OpenGL).");
            }
        }

        /// <summary>
        /// Reads a plain-text HLSL assembly shader (.vsh or .psh) and writes it
        /// to the output file with the standard shader header prepended.
        /// </summary>
        private static void DecompileTextShader(string inputFile, string outputFile)
        {
            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string shaderText = File.ReadAllText(inputFile, Encoding.UTF8);
            File.WriteAllText(outputFile, Header.Shaders + shaderText);
        }

        private static void DecompileFxc(string inputFile, string outputFile)
        {
            if (Shaders.IsWindowsPlatform)
            {
                DecompileFxcWithD3DDisassemble(inputFile, outputFile);
            }
            else
            {
                DecompileFxcWithDxbcDisassembler(inputFile, outputFile);
            }
        }

        private static void DecompileFxcWithD3DDisassemble(string inputFile, string outputFile)
        {
            try
            {
                byte[] shaderBytecode = File.ReadAllBytes(inputFile);
#pragma warning disable CA1416
                string disassembledCode = Shaders.DisassembleFxcWithD3DDisassemble(shaderBytecode);
#pragma warning restore CA1416
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

        private static bool IsOpenGLShader(string inputFile)
        {
            try
            {
                using var reader = new StreamReader(inputFile, Encoding.UTF8);
                string firstLine = reader.ReadLine() ?? string.Empty;
                return firstLine.StartsWith("#version", StringComparison.OrdinalIgnoreCase) ||
                       firstLine.Contains("GLSL", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void DecompileGlslWithSpirvCross(string inputFile, string outputFile)
        {
            string disassemblerPath = GetOpenGLDisassemblerPath();

            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = disassemblerPath,
                    Arguments = $"--es --output \"{outputFile}\" \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            // Read both streams concurrently to prevent deadlocks when buffers fill up.
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(10000))
            {
                process.Kill();
                throw new InvalidOperationException(
                    "SPIRV-Cross process timed out after 10 seconds.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to decompile GLSL shader. Exit code: {process.ExitCode}\n" +
                    $"Error output: {(string.IsNullOrEmpty(standardError) ? "(none)" : standardError)}");
            }

            // spirv-cross writes to the output file directly via --output; read it back.
            string disassembledCode = File.Exists(outputFile)
                ? File.ReadAllText(outputFile)
                : standardOutput;

            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static void DecompileFxcWithDxbcDisassembler(string inputFile, string outputFile)
        {
            string disassemblerPath = GetDirectXDisassemblerPath();

            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

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

        private static string GetOpenGLDisassemblerPath()
        {
            string[] absolutePaths =
            {
                "/usr/bin/spirv-cross",
                "/usr/local/bin/spirv-cross",
                Path.Combine(AppContext.BaseDirectory, "spirv-cross"),
            };

            foreach (string path in absolutePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Fall back to letting the OS resolve via PATH.
            return "spirv-cross";
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
