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
        public static event Action<string>? LogMessage;

        public static void Decompile(string inputFile, string? outputFile = null, string? gameVersion = null)
        {
            LogMessage?.Invoke($"Starting decompilation of {Path.GetFileName(inputFile)}");

            if (!File.Exists(inputFile))
            {
                LogMessage?.Invoke($"Error: Input file not found: {inputFile}");
                throw new FileNotFoundException("Input file not found.", inputFile);
            }

            if (string.IsNullOrEmpty(gameVersion))
            {
                gameVersion = "VanillaWarband";
                LogMessage?.Invoke("No game version specified, defaulting to VanillaWarband");
            }

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
                    _ => throw new ArgumentException($"Game version not supported: {gameVersion}")
                };
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error: {ex.Message}");
                throw;
            }

            string extension = Path.GetExtension(inputFile).ToLowerInvariant();

            if (extension is ".vsh" or ".psh" or ".fxc" or ".glsl")
            {
                string outputPath = outputFile ?? Path.ChangeExtension(inputFile, ".txt");
                ShaderDecompiler.Decompile(inputFile, outputPath, gameVersion);
                LogMessage?.Invoke($"Shader decompiled to {outputPath}");
                return;
            }

            try
            {
                var operators = version.GetOperators()
                    .GroupBy(op => op.Code)
                    .ToDictionary(g => g.Key, g => g.First());

                using Text text = new Text(inputFile);
                using FileWriter output = outputFile == null
                    ? new FileWriter(Console.Out)
                    : new FileWriter(outputFile);

                while (text.Peek() != -1)
                {
                    int iRecords = text.GetInt();
                    output.WriteLine("decl {0}", iRecords);
                    LogMessage?.Invoke($"Found {iRecords} records");

                    for (int r = 0; r < iRecords; r++)
                    {
                        int iCodeSize = text.GetInt();
                        output.WriteLine("code {0}", iCodeSize);

                        for (int i = 0; i < iCodeSize; i++)
                        {
                            ulong iOpCode = text.GetUInt64();
                            int iOpCodeInt = (int)(iOpCode & 0xFFFFFFFF);

                            if (!operators.TryGetValue(iOpCodeInt, out var op))
                            {
                                output.WriteLine("\tunknown_opcode_{0}", iOpCodeInt);
                                LogMessage?.Invoke($"Warning: Unknown opcode {iOpCodeInt}");
                                continue;
                            }

                            output.Write("\t{0}", op.Value);
                            int iNumParams = op.Parameters.Count;

                            for (int p = 0; p < iNumParams; p++)
                            {
                                ulong iParam = text.GetUInt64();
                                output.Write(" {0}", op.GetParameter(p, iParam.ToString()));
                            }
                            output.WriteLine();
                        }
                    }
                }

                LogMessage?.Invoke("Decompilation completed successfully");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error during decompilation: {ex.Message}");
                throw;
            }
        }
    }
}
