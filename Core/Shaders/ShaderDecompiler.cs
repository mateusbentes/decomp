using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Decomp.Core.Shaders
{
    public static class ShaderDecompiler
    {
        public static void Decompile(string inputFile, string outputFile)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Shaders.Decompile(inputFile);
            }
            else
            {
                DecompileWithExternalTool(inputFile, outputFile);
            }
        }

        private static void DecompileWithExternalTool(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"Arquivo de shader não encontrado: {inputFile}");
            }

            string disassemblerPath = GetDisassemblerPath();
            if (!File.Exists(disassemblerPath))
            {
                throw new FileNotFoundException(
                    $"Ferramenta de decompilação de shaders não encontrada em: {disassemblerPath}\n" +
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
                throw new InvalidOperationException($"Falha ao decompilar shader. Código de saída: {process.ExitCode}");
            }

            File.WriteAllText(outputFile, Header.Shaders + disassembledCode);
        }

        private static string GetDisassemblerPath()
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
