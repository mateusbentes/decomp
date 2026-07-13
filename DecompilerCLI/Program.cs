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
                Console.WriteLine("Usage: DecompilerCLI <input_file> [output_file]");
                return;
            }

            string inputFile = args[0];
            string outputFile = args.Length > 1 ? args[1] : null;

            try
            {
                Decompiler.StartDecompilation(inputFile, outputFile);
                Console.WriteLine("Decompilation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompilation: {ex.Message}");
            }
        }
    }
}
