using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Decomp.Core.Shaders;

/// <summary>
/// Decompiles TaleWorlds shader resources across Windows, macOS, and Linux.
/// </summary>
public static class ShaderDecompiler
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static void Decompile(string inputFile, string outputFile, string? gameVersion = null)
    {
        DecompileAsync(inputFile, outputFile, gameVersion).GetAwaiter().GetResult();
    }

    public static async Task DecompileAsync(
        string inputFile,
        string outputFile,
        string? gameVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);

        if (!File.Exists(inputFile))
            throw new FileNotFoundException("Shader file not found.", inputFile);

        var extension = Path.GetExtension(inputFile);
        if (extension is ".vsh" or ".psh" or ".glsl")
        {
            await DecompileTextShaderAsync(inputFile, outputFile, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (extension.Equals(".fxc", StringComparison.OrdinalIgnoreCase))
        {
            await DecompileFxcAsync(inputFile, outputFile, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException(
            $"Shader decompilation for file type '{extension}' is not supported. " +
            "Supported formats: .vsh/.psh/.glsl (text shaders) and .fxc (DirectX bytecode).");
    }

    private static async Task DecompileTextShaderAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        await EnsureOutputDirectoryAsync(outputFile, cancellationToken).ConfigureAwait(false);
        var shaderText = await File.ReadAllTextAsync(inputFile, Utf8, cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(
            outputFile,
            Header.Shaders + shaderText,
            Utf8,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task DecompileFxcAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        if (Shaders.IsWindowsPlatform)
        {
            try
            {
                var shaderBytecode = await File.ReadAllBytesAsync(inputFile, cancellationToken)
                    .ConfigureAwait(false);
#pragma warning disable CA1416
                var disassembledCode = await Task.Run(
                    () => Shaders.DisassembleFxcWithD3DDisassemble(shaderBytecode),
                    cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1416
                await EnsureOutputDirectoryAsync(outputFile, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    outputFile,
                    Header.Shaders + disassembledCode,
                    Utf8,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (DllNotFoundException)
            {
                // Fall back to the portable disassembler when DirectX is unavailable.
            }
            catch (InvalidOperationException exception)
                when (exception.Message.Contains("d3dcompiler_47.dll", StringComparison.OrdinalIgnoreCase))
            {
                // Fall back to the portable disassembler when DirectX is unavailable.
            }
        }

        await DecompileFxcWithDxbcDisassemblerAsync(inputFile, outputFile, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task DecompileFxcWithDxbcDisassemblerAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        await EnsureOutputDirectoryAsync(outputFile, cancellationToken).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetDirectXDisassemblerPath(),
                ArgumentList = { "-disassemble", inputFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Unable to start dxbc-disassembler.");

            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var disassembledCode = await outputTask.ConfigureAwait(false);
            var standardError = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to disassemble .fxc file. Exit code: {process.ExitCode}. " +
                    $"Error output: {(string.IsNullOrWhiteSpace(standardError) ? "(none)" : standardError)}");
            }

            await File.WriteAllTextAsync(
                outputFile,
                Header.Shaders + disassembledCode,
                Utf8,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new InvalidOperationException("dxbc-disassembler timed out after 10 seconds.");
        }
        catch
        {
            TryKill(process);
            throw;
        }
    }

    private static async Task EnsureOutputDirectoryAsync(
        string outputFile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile));
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            await Task.Run(() => Directory.CreateDirectory(outputDirectory), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string GetDirectXDisassemblerPath()
    {
        string[] absolutePaths =
        [
            Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler.exe"),
            Path.Combine(AppContext.BaseDirectory, "dxbc-disassembler"),
        ];

        foreach (var path in absolutePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "dxbc-disassembler.exe"
            : "dxbc-disassembler";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process may have exited between the check and the kill request.
        }
    }
}
