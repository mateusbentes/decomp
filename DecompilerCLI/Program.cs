using System;
using Decomp.Core;

namespace DecompilerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: DecompilerCLI <input_file> [output_file] [game_version]");
                Console.WriteLine("Game versions: VanillaClassic, VanillaWarband, WSE320, WSE450, Caribbean");
                return;
            }

            string inputFile = args[0];
            string outputFile = args.Length > 1 ? args[1] : null;
            string gameVersion = args.Length > 2 ? args[2] : "VanillaWarband";

            try
            {
                Decompiler.Decompile(inputFile, outputFile, gameVersion);
                Console.WriteLine("Decompilation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompilation: {ex.Message}");
            }
        }
    }
}
