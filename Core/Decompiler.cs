using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Decomp.Core.Operators;
using Decomp.Core.Shaders;

namespace Decomp.Core
{
    public static class Decompiler
    {
        public static event Action<string>? OnLogMessage;

        public static void Decompile(string inputFilePath, string? outputFilePath = null, string? gameVersion = null)
        {
            OnLogMessage?.Invoke($"Starting decompilation of {Path.GetFileName(inputFilePath)}");

            if (!File.Exists(inputFilePath))
            {
                OnLogMessage?.Invoke($"Error: Input file not found: {inputFilePath}");
                throw new FileNotFoundException("Input file not found.", inputFilePath);
            }

            gameVersion ??= "VanillaWarband";
            OnLogMessage?.Invoke($"Game version: {gameVersion}");

            IGameVersion version;
            try
            {
                version = gameVersion.ToLowerInvariant() switch
                {
                    "vanillaclassic" => new VanillaVersion(),
                    "vanillawarband" => new Warband1153Version(),
                    "warband1171" => new Warband1171Version(),
                    "vanillawfs" => new WFSVersion(),
                    "wse320" => new WarbandScriptEnhancer320Version(),
                    "wse450" => new WarbandScriptEnhancer450Version(),
                    "caribbean" => new CaribbeanVersion(),
                    _ => throw new ArgumentException($"Unsupported game version: {gameVersion}")
                };
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error: {ex.Message}");
                throw;
            }

            var extension = Path.GetExtension(inputFilePath).ToLowerInvariant();
            if (extension is ".vsh" or ".psh" or ".fxc" or ".glsl")
            {
                var outputPath = outputFilePath ?? Path.ChangeExtension(inputFilePath, ".txt");
                ShaderDecompiler.Decompile(inputFilePath, outputPath, gameVersion);
                OnLogMessage?.Invoke($"Shader decompiled to {outputPath}");
                return;
            }

            try
            {
                var operators = Operator.GetCollection(version.GetOperators())
                    .GroupBy(op => op.Code)
                    .ToDictionary(g => g.Key, g => g.First());

                Common.Operators = operators;

                using var input = new Text(inputFilePath);
                using var output = outputFilePath == null
                    ? new FileWriter(Console.Out)
                    : new FileWriter(outputFilePath);

                while (input.Peek() != -1)
                {
                    var recordCount = input.ReadInt();
                    output.WriteLine($"decl {recordCount}");
                    OnLogMessage?.Invoke($"Found {recordCount} records");

                    for (var r = 0; r < recordCount; r++)
                    {
                        var codeSize = input.ReadInt();
                        output.WriteLine($"code {codeSize}");

                        for (var i = 0; i < codeSize; i++)
                        {
                            var opcode = input.ReadUInt64();
                            var opcodeInt = (int)(opcode & 0xFFFFFFFF);

                            if (!operators.TryGetValue(opcodeInt, out var op))
                            {
                                output.WriteLine($"\tunknown_opcode_{opcodeInt}");
                                OnLogMessage?.Invoke($"Warning: Unknown opcode {opcodeInt}");
                                continue;
                            }

                            output.Write($"\t{op.Value}");
                            var parameterCount = op.Parameters.Count;

                            for (var p = 0; p < parameterCount; p++)
                            {
                                var parameter = input.ReadUInt64();
                                output.Write($" {op.GetParameter(p, parameter.ToString())}");
                            }
                            output.WriteLine();
                        }
                    }
                }

                OnLogMessage?.Invoke("Decompilation completed successfully");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error during decompilation: {ex.Message}");
                throw;
            }
        }
    }
}
