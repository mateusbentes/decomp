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
                    "Supported formats: .fxc (DirectX), .glsl (OpenGL).");
            }
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
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"SPIRV-Cross tool not found at: {disassemblerPath}\n" +
                    "Install spirv-cross (https://github.com/KhronosGroup/SPIRV-Cross) and ensure it is in the PATH or project directory.");
            }

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

            try
            {
                process.Start();
                string standardError = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new InvalidOperationException(
                        $"SPIRV-Cross process timed out after 10 seconds.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to decompile GLSL shader. Exit code: {process.ExitCode}\n" +
                        $"Error output: {standardError}");
                }

                string disassembledCode = File.ReadAllText(outputFile);
                File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
            }
            finally
            {
                process.Dispose();
            }
        }

        private static void DecompileFxcWithDxbcDisassembler(string inputFile, string outputFile)
        {
            string disassemblerPath = GetDirectXDisassemblerPath();
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"dxbc-disassembler not found at: {disassemblerPath}\n" +
                    "Install dxbc-disassembler (https://github.com/microsoft/DirectXShaderCompiler) and ensure it is in the PATH or project directory.");
            }

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

            try
            {
                process.Start();
                string disassembledCode = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new InvalidOperationException(
                        $"dxbc-disassembler process timed out after 10 seconds.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to disassemble .fxc file. Exit code: {process.ExitCode}\n" +
                        $"Error output: {standardError}");
                }

                File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
            }
            finally
            {
                process.Dispose();
            }
        }

        private static string GetOpenGLDisassemblerPath()
        {
            string[] possiblePaths =
            {
                "spirv-cross",
                Path.Combine(AppContext.BaseDirectory, "spirv-cross"),
                "/usr/bin/spirv-cross",
                "/usr/local/bin/spirv-cross"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return "spirv-cross";
        }

        private static string GetDirectXDisassemblerPath()
        {
            string[] possiblePaths =
            {
                "dxbc-disassembler",
                "dxbc-disassembler.exe",
                Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler"),
                Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler.exe")
            };

            foreach (string path in possiblePaths)
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
