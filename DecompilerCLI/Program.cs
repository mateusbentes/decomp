using System;
using System.IO;
using Decomp.Core;

namespace DecompilerCLI
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string inputPath = args[0];
            string? outputPath = args.Length > 1 ? args[1] : null;
            string? gameVersion = args.Length > 2 ? args[2] : null;

            try
            {
                if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
                {
                    Console.Error.WriteLine($"Error: Input path '{inputPath}' does not exist.");
                    return 1;
                }

                if (Directory.Exists(inputPath))
                {
                    return DecompileFolder(inputPath, outputPath, gameVersion);
                }

                if (File.Exists(inputPath))
                {
                    return DecompileSingleFile(inputPath, outputPath, gameVersion);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during decompilation: {ex.Message}");
                return 1;
            }
        }

        private static int DecompileFolder(string inputPath, string? outputPath, string? gameVersion)
        {
            if (outputPath == null)
            {
                outputPath = Path.Combine(inputPath, "decompiled");
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string[] files = Directory.GetFiles(inputPath);
            int processedFiles = 0;
            int failedFiles = 0;

            foreach (string file in files)
            {
                try
                {
                    string outputFile = Path.Combine(
                        outputPath,
                        Path.GetFileNameWithoutExtension(file) + ".txt");

                    Decompiler.Decompile(file, outputFile, gameVersion);
                    processedFiles++;
                }
                catch (PlatformNotSupportedException ex)
                {
                    Console.Error.WriteLine($"Warning: Platform not supported for '{file}': {ex.Message}");
                    failedFiles++;
                }
                catch (NotSupportedException ex)
                {
                    Console.Error.WriteLine($"Warning: File type not supported for '{file}': {ex.Message}");
                    failedFiles++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to decompile '{file}': {ex.Message}");
                    failedFiles++;
                }
            }

            Console.WriteLine(
                $"Decompilation complete. Processed: {processedFiles}, Failed: {failedFiles}, Total: {files.Length}.");

            return failedFiles > 0 && processedFiles == 0 ? 1 : 0;
        }

        private static int DecompileSingleFile(string inputPath, string? outputPath, string? gameVersion)
        {
            string outputFile;

            if (outputPath == null)
            {
                outputFile = Path.ChangeExtension(inputPath, ".txt");
            }
            else if (Directory.Exists(outputPath))
            {
                outputFile = Path.Combine(
                    outputPath,
                    Path.GetFileNameWithoutExtension(inputPath) + ".txt");
            }
            else
            {
                outputFile = outputPath;
                string? outputDirectory = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
            }

            try
            {
                Decompiler.Decompile(inputPath, outputFile, gameVersion);
                Console.WriteLine($"Decompilation complete: '{outputFile}'");
                return 0;
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.Error.WriteLine($"Error: Platform not supported: {ex.Message}");
                return 1;
            }
            catch (NotSupportedException ex)
            {
                Console.Error.WriteLine($"Error: File type not supported: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to decompile '{inputPath}': {ex.Message}");
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DecompilerCLI <input_file_or_folder> [output_file_or_folder] [game_version]");
            Console.WriteLine();
            Console.WriteLine("Supported game versions:");
            Console.WriteLine("  VanillaClassic    - Mount & Blade Classic");
            Console.WriteLine("  VanillaWarband    - Mount & Blade: Warband (1.153)  [default]");
            Console.WriteLine("  Warband1171       - Mount & Blade: Warband (1.171)");
            Console.WriteLine("  VanillaWFS        - Mount & Blade: With Fire & Sword");
            Console.WriteLine("  WSE320            - Warband Script Enhancer 3.2.0");
            Console.WriteLine("  WSE450            - Warband Script Enhancer 4.5.0");
            Console.WriteLine("  Caribbean         - Mount & Blade: Caribbean");
            Console.WriteLine();
            Console.WriteLine("Supported shader formats:");
            Console.WriteLine("  .vsh / .psh       - DirectX text assembly (plain-text, no tool required)");
            Console.WriteLine("  .fxc              - DirectX binary bytecode (requires d3dcompiler_47.dll on Windows");
            Console.WriteLine("                      or dxbc-disassembler on Linux/macOS)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  DecompilerCLI scripts.txt");
            Console.WriteLine("  DecompilerCLI scripts.txt output.txt VanillaWarband");
            Console.WriteLine("  DecompilerCLI ./module_folder ./decompiled Caribbean");
        }
    }
}
