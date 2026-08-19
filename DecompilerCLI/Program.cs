using Decomp.Core;

namespace DecompilerCLI;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Length > 3)
        {
            PrintUsage();
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
            Console.Error.WriteLine("Cancellation requested. Finishing the current operation...");
        };

        try
        {
            var inputPath = Decompiler.NormalizePath(args[0]);
            var gameVersion = args.Length > 2 ? args[2] : "VanillaWarband";

            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                Console.Error.WriteLine($"Error: Input path '{inputPath}' does not exist.");
                return 1;
            }

            var outputPath = ResolveOutputPath(inputPath, args.Length > 1 ? args[1] : null);
            var progress = new Progress<DecompilationProgress>(ReportProgress);

            Console.WriteLine($"Starting decompilation with game version '{gameVersion}'.");
            await Decompiler.DecompileAsync(
                inputPath,
                outputPath,
                gameVersion,
                progress,
                cancellation.Token).ConfigureAwait(false);
            Console.WriteLine("Decompilation completed successfully.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Decompilation canceled.");
            return 2;
        }
        catch (PlatformNotSupportedException exception)
        {
            Console.Error.WriteLine($"Error: Platform not supported: {exception.Message}");
            return 1;
        }
        catch (NotSupportedException exception)
        {
            Console.Error.WriteLine($"Error: File type or game version not supported: {exception.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error during decompilation: {exception.Message}");
            if (exception.InnerException is not null)
                Console.Error.WriteLine($"Details: {exception.InnerException.Message}");
            return 1;
        }
    }

    private static string ResolveOutputPath(string inputPath, string? requestedOutputPath)
    {
        if (string.IsNullOrWhiteSpace(requestedOutputPath))
        {
            var inputDirectory = Directory.Exists(inputPath)
                ? inputPath
                : Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
            return Path.Combine(inputDirectory, "decompiled");
        }

        var normalizedOutput = Decompiler.NormalizePath(requestedOutputPath);
        if (Directory.Exists(inputPath) || !Path.GetExtension(inputPath).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return normalizedOutput;

        // Module text files produce several Python files, so a file input always targets a directory.
        return Directory.Exists(normalizedOutput) || string.IsNullOrEmpty(Path.GetExtension(normalizedOutput))
            ? normalizedOutput
            : Path.GetDirectoryName(normalizedOutput) ?? Directory.GetCurrentDirectory();
    }

    private static void ReportProgress(DecompilationProgress update)
    {
        if (update.TotalFiles > 0)
        {
            Console.WriteLine($"[{update.ProcessedFiles}/{update.TotalFiles}] {update.Message}");
            return;
        }

        Console.WriteLine(update.Message);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Warband Module Decompiler");
        Console.WriteLine();
        Console.WriteLine("Usage: DecompilerCLI <input_file_or_folder> [output_folder] [game_version]");
        Console.WriteLine();
        Console.WriteLine("Supported platforms: Windows, macOS, and Linux");
        Console.WriteLine("Supported game versions: VanillaClassic, VanillaWarband, Warband1171,");
        Console.WriteLine("  VanillaWFS, WSE320, WSE450, and Caribbean");
        Console.WriteLine("Supported formats: .txt, .vsh, .psh, .fxc, and .glsl");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  DecompilerCLI scripts.txt");
        Console.WriteLine("  DecompilerCLI ./Native ./src_python VanillaWarband");
    }
}
