using System;
using System.IO;
using Decomp.Core;

namespace DecompilerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: DecompilerCLI <input_path> [output_path] [game_version]");
                Console.WriteLine("  input_path:  Caminho para um arquivo .txt ou pasta contendo arquivos para decompilar.");
                Console.WriteLine("  output_path: (Opcional) Pasta de saída. Se não fornecido, usa a pasta de entrada.");
                Console.WriteLine("  game_version: (Opcional) Versão do jogo. Padrão: VanillaWarband");
                Console.WriteLine("\nGame versions:");
                Console.WriteLine("  VanillaClassic    - Mount & Blade Classic");
                Console.WriteLine("  VanillaWarband    - Mount & Blade: Warband");
                Console.WriteLine("  VanillaWFS        - Mount & Blade: With Fire & Sword");
                Console.WriteLine("  WSE320            - Warband Script Enhancer 3.2.0");
                Console.WriteLine("  WSE450            - Warband Script Enhancer 4.5.0");
                Console.WriteLine("  Caribbean         - Mount & Blade: Caribbean");
                return;
            }

            string inputPath = args[0];
            string outputPath = args.Length > 1 ? args[1] : null;
            string gameVersion = args.Length > 2 ? args[2] : "VanillaWarband";

            try
            {
                if (Directory.Exists(inputPath))
                {
                    // Processar todos os arquivos .txt na pasta
                    string[] files = Directory.GetFiles(inputPath, "*.txt");
                    if (files.Length == 0)
                    {
                        Console.WriteLine("Nenhum arquivo .txt encontrado na pasta de entrada.");
                        return;
                    }

                    outputPath ??= inputPath; // Usar pasta de entrada se não fornecida
                    Directory.CreateDirectory(outputPath); // Garantir que a pasta de saída exista

                    foreach (string file in files)
                    {
                        string outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(file) + "_decompiled.txt");
                        Decompiler.Decompile(file, outputFile, gameVersion);
                        Console.WriteLine($"Decompilado: {file} -> {outputFile}");
                    }
                }
                else if (File.Exists(inputPath))
                {
                    // Processar arquivo único
                    outputPath ??= Path.GetDirectoryName(inputPath);
                    string outputFile = Path.Combine(
                        outputPath,
                        Path.GetFileNameWithoutExtension(inputPath) + "_decompiled.txt"
                    );
                    Decompiler.Decompile(inputPath, outputFile, gameVersion);
                    Console.WriteLine($"Decompilado: {inputPath} -> {outputFile}");
                }
                else
                {
                    Console.WriteLine("Caminho de entrada inválido. Deve ser um arquivo ou pasta.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a decompilação: {ex.Message}");
            }
        }
    }
}
