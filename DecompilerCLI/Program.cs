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
                Console.WriteLine("Usage: DecompilerCLI <input_file_or_folder> [output_file_or_folder] [game_version]");
                Console.WriteLine("Supported game versions:");
                Console.WriteLine("  VanillaClassic    - Mount & Blade Classic");
                Console.WriteLine("  VanillaWarband    - Mount & Blade: Warband (1.153)");
                Console.WriteLine("  Warband1171       - Mount & Blade: Warband (1.171)");
                Console.WriteLine("  VanillaWFS        - Mount & Blade: With Fire & Sword");
                Console.WriteLine("  WSE320            - Warband Script Enhancer 3.2.0");
                Console.WriteLine("  WSE450            - Warband Script Enhancer 4.5.0");
                Console.WriteLine("  Caribbean         - Mount & Blade: Caribbean");
                return 1;
            }

            string inputPath = args[0];
            string? outputPath = args.Length > 1 ? args[1] : null;
            string? gameVersion = args.Length > 2 ? args[2] : null;

            try
            {
                if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
                {
                    Console.WriteLine($"Error: Input path '{inputPath}' does not exist.");
                    return 1;
                }

                if (Directory.Exists(inputPath))
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

                    foreach (string file in files)
                    {
                        try
                        {
                            string outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(file) + ".txt");
                            Decompiler.Decompile(file, outputFile, gameVersion);
                            processedFiles++;
                        }
                        catch (PlatformNotSupportedException ex)
                        {
                            Console.WriteLine($"Warning: {ex.Message} (file: {file})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to decompile file '{file}': {ex.Message}");
                        }
                    }

                    Console.WriteLine($"Decompilation of folder completed successfully. Processed {processedFiles} files.");
                }
                else if (File.Exists(inputPath))
                {
                    string outputFile;
                    if (outputPath == null)
                    {
                        outputFile = Path.ChangeExtension(inputPath, ".txt");
                    }
                    else if (Directory.Exists(outputPath))
                    {
                        outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputPath) + ".txt");
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
                        Console.WriteLine("Decompilation completed successfully.");
                    }
                    catch (PlatformNotSupportedException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return 1;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompilation: {ex.Message}");
                return 1;
            }
        }
    }
}
