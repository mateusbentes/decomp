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
                throw new FileNotFoundException($"Arquivo de shader não encontrado: {inputFile}");
            }

            if (string.IsNullOrEmpty(gameVersion))
            {
                gameVersion = "VanillaWarband";
            }

            bool isWarband = gameVersion.Equals("VanillaWarband", StringComparison.OrdinalIgnoreCase) ||
                            gameVersion.Equals("WSE320", StringComparison.OrdinalIgnoreCase) ||
                            gameVersion.Equals("WSE450", StringComparison.OrdinalIgnoreCase);

            // Detecta automaticamente se o shader é OpenGL (Warband) ou Direct3D (outros jogos)
            bool isOpenGLShader = IsOpenGLShader(inputFile);

            if (isWarband && isOpenGLShader && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                // Shaders OpenGL (Warband) geralmente começam com "#version" ou têm cabeçalhos específicos
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
                    $"Ferramenta de decompilação OpenGL não encontrada em: {disassemblerPath}\n" +
                    "Instale o spirv-cross (https://github.com/KhronosGroup/SPIRV-Cross) e coloque-o no PATH ou na pasta do projeto.");
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
                throw new InvalidOperationException($"Falha ao decompilar shader OpenGL. Código de saída: {process.ExitCode}");
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
                    $"Ferramenta de decompilação DirectX não encontrada em: {disassemblerPath}\n" +
                    "Instale o dxbc-disassembler (https://github.com/microsoft/DirectXShaderCompiler) e coloque-o no PATH ou na pasta do projeto.");
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
                throw new InvalidOperationException($"Falha ao decompilar shader DirectX. Código de saída: {process.ExitCode}");
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
