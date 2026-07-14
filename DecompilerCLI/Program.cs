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
                Console.WriteLine("  VanillaWarband    - Mount & Blade: Warband");
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
                if (Directory.Exists(inputPath))
                {
                    // Process folder
                    string[] files = Directory.GetFiles(inputPath);
                    foreach (string file in files)
                    {
                        string outputFile = outputPath != null
                            ? Path.Combine(outputPath, Path.GetFileNameWithoutExtension(file) + ".txt")
                            : Path.ChangeExtension(file, ".txt");
                        Decompiler.Decompile(file, outputFile, gameVersion);
                    }
                    Console.WriteLine("Decompilation of folder completed successfully.");
                }
                else if (File.Exists(inputPath))
                {
                    // Process single file
                    string outputFile = outputPath != null
                        ? (Directory.Exists(outputPath)
                            ? Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputPath) + ".txt")
                            : outputPath)
                        : Path.ChangeExtension(inputPath, ".txt");
                    Decompiler.Decompile(inputPath, outputFile, gameVersion);
                    Console.WriteLine("Decompilation completed successfully.");
                }
                else
                {
                    Console.WriteLine($"Error: Input path '{inputPath}' does not exist.");
                    return 1;
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
