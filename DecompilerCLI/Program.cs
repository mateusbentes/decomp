using System;
using Decomp.Core;

namespace DecompilerCLI
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("E aí, beleza? Precisa descompilar um arquivo?");
                Console.WriteLine("Modo de usar: DecompilerCLI <arquivo_entrada> [arquivo_saida] [versão_jogo]");
                Console.WriteLine("Versões de jogo suportadas:");
                Console.WriteLine("  VanillaClassic    - Mount & Blade Classic");
                Console.WriteLine("  VanillaWarband    - Mount & Blade: Warband");
                Console.WriteLine("  VanillaWFS        - Mount & Blade: With Fire & Sword");
                Console.WriteLine("  WSE320            - Warband Script Enhancer 3.2.0");
                Console.WriteLine("  WSE450            - Warband Script Enhancer 4.5.0");
                Console.WriteLine("  Caribbean         - Mount & Blade: Caribbean");
                return 1;
            }

            string inputFile = args[0];
            string? outputFile = args.Length > 1 ? args[1] : null;
            string? gameVersion = args.Length > 2 ? args[2] : null;

            try
            {
                Decompiler.Decompile(inputFile, outputFile, gameVersion);
                Console.WriteLine("Decompilação concluída com sucesso!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a decompilação: {ex.Message}");
                return 1;
            }
        }
    }
}
