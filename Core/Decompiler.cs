using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Decomp.Core.Operators;
using Decomp.Core.Shaders;

namespace Decomp.Core
{
    public static class Decompiler
    {
        public static void Decompile(string inputFile, string? outputFile = null, string? gameVersion = null)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException("Input file not found.", inputFile);
            }

            if (string.IsNullOrEmpty(gameVersion))
            {
                gameVersion = "VanillaWarband";
            }
            else if (gameVersion.Equals("VanillaWFS", StringComparison.OrdinalIgnoreCase))
            {
                gameVersion = "VanillaWarband";
            }

            IGameVersion version = gameVersion.ToLowerInvariant() switch
            {
                "vanillaclassic" => new VanillaVersion(),
                "vanillawarband" => new Warband1153Version(),
                "wse320" => new WarbandScriptEnhancer320Version(),
                "wse450" => new WarbandScriptEnhancer450Version(),
                "caribbean" => new CaribbeanVersion(),
                _ => throw new ArgumentException($"Game version not supported: {gameVersion}")
            };

            string extension = Path.GetExtension(inputFile).ToLowerInvariant();
            if (extension is ".fx" or ".vsh" or ".psh")
            {
                string outputPath = outputFile ?? Path.ChangeExtension(inputFile, ".txt");
                ShaderDecompiler.Decompile(inputFile, outputPath, gameVersion);
                return;
            }

            var operators = version.GetOperators().ToDictionary(op => op.Code, op => op);

            using Text text = new Text(inputFile);
            using FileWriter output = outputFile == null
                ? new FileWriter(Console.Out)
                : new FileWriter(outputFile);

            // Do NOT manually call Dispose here; the using declarations above handle it.
            while (text.Peek() != -1)
            {
                int iRecords = text.GetInt();
                output.WriteLine("decl {0}", iRecords);

                for (int r = 0; r < iRecords; r++)
                {
                    int iCodeSize = text.GetInt();
                    output.WriteLine("code {0}", iCodeSize);

                    for (int i = 0; i < iCodeSize; i++)
                    {
                        int iOpCode = text.GetInt();
                        if (!operators.TryGetValue(iOpCode, out var op))
                        {
                            output.WriteLine("\tunknown_opcode_{0}", iOpCode);
                            continue;
                        }

                        output.Write("\t{0}", op.Value);
                        int iNumParams = op.Parameters.Count;

                        for (int p = 0; p < iNumParams; p++)
                        {
                            int iParam = text.GetInt();
                            output.Write(" {0}", op.GetParameter(p, iParam.ToString()));
                        }
                        output.WriteLine();
                    }
                }
            }
        }
    }
}
