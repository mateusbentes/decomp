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

            bool isWarband = gameVersion.Equals("VanillaWarband", StringComparison.OrdinalIgnoreCase) ||
                            gameVersion.Equals("WSE320", StringComparison.OrdinalIgnoreCase) ||
                            gameVersion.Equals("WSE450", StringComparison.OrdinalIgnoreCase);

            // Detect if the input is a compiled .fxc file (DirectX binary)
            bool isFxcFile = Path.GetExtension(inputFile).Equals(".fxc", StringComparison.OrdinalIgnoreCase);

            if (isFxcFile)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use native Direct3D APIs on Windows
                    Shaders.DecompileFxc(inputFile, outputFile);
                }
                else
                {
                    // Use dxbc-disassembler on Linux/macOS
                    DecompileFxcWithDxbcDisassembler(inputFile, outputFile);
                }
            }
            else if (isWarband && IsOpenGLShader(inputFile) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DecompileWarbandOpenGLShader(inputFile, outputFile);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Shaders.Decompile(inputFile);
            }
            else
            {
                DecompileDirectXShader(inputFile, outputFile);
            }
        }

        private static bool IsOpenGLShader(string inputFile)
        {
            try
            {
                // OpenGL shaders (Warband) usually start with "#version" or have GLSL headers
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

        private static void DecompileWarbandOpenGLShader(string inputFile, string outputFile)
        {
            string disassemblerPath = GetOpenGLDisassemblerPath();
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"OpenGL disassembler tool not found at: {disassemblerPath}\n" +
                    "Install spirv-cross (https://github.com/KhronosGroup/SPIRV-Cross) and ensure it is in the PATH or project directory.");
            }

            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = disassemblerPath,
                    Arguments = $"--es --output {outputFile} \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to decompile OpenGL shader. Exit code: {process.ExitCode}");
            }

            string disassembledCode = File.ReadAllText(outputFile);
            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static void DecompileDirectXShader(string inputFile, string outputFile)
        {
            string disassemblerPath = GetDirectXDisassemblerPath();
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"DirectX disassembler tool not found at: {disassemblerPath}\n" +
                    "Install dxbc-disassembler (https://github.com/microsoft/DirectXShaderCompiler) and ensure it is in the PATH or project directory.");
            }

            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = disassemblerPath,
                    Arguments = $"-disassemble \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string disassembledCode = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to decompile DirectX shader. Exit code: {process.ExitCode}");
            }

            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static void DecompileFxcWithDxbcDisassembler(string inputFile, string outputFile)
        {
            string disassemblerPath = GetDirectXDisassemblerPath();
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"dxbc-disassembler not found at: {disassemblerPath}\n" +
                    "Please follow the Shader Decompilation Instructions to install the required tools:\n" +
                    "https://github.com/YourProject/ShaderDecompilationGuide");
            }

            string? outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = disassemblerPath,
                    Arguments = $"-disassemble \"{inputFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string disassembledCode = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to disassemble .fxc file. Exit code: {process.ExitCode}");
            }

            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static string GetOpenGLDisassemblerPath()
        {
            string[] possiblePaths =
            {
                "spirv-cross",
                Path.Combine(AppContext.BaseDirectory, "spirv-cross")
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

            return "dxbc-disassembler";
        }
    }
}
