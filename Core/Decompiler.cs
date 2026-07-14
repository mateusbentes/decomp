using System;
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

            IGameVersion version = gameVersion.ToLowerInvariant() switch
            {
                "vanillaclassic" => new VanillaVersion(),
                "vanillawarband" => new Warband1153Version(),
                "warband1171"    => new Warband1171Version(),
                "vanillawfs"     => new WFSVersion(),
                "wse320"         => new WarbandScriptEnhancer320Version(),
                "wse450"         => new WarbandScriptEnhancer450Version(),
                "caribbean"      => new CaribbeanVersion(),
                _ => throw new ArgumentException($"Game version not supported: {gameVersion}")
            };

            string extension = Path.GetExtension(inputFile).ToLowerInvariant();

            // .vsh (vertex shader) and .psh (pixel shader) are plain-text HLSL assembly
            // files used by the M&B engine. ShaderDecompiler handles them by reading the
            // text and prepending the standard shader header.
            // .fxc and .glsl are compiled/binary shaders that require actual decompilation.
            if (extension is ".vsh" or ".psh" or ".fxc" or ".glsl")
            {
                string outputPath = outputFile ?? Path.ChangeExtension(inputFile, ".txt");
                ShaderDecompiler.Decompile(inputFile, outputPath, gameVersion);
                return;
            }

            // Use GroupBy + First to safely handle duplicate opcode values
            // (e.g. "tutorial_box" and "dialog_box" both use code 1120 in Caribbean).
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

                for (int r = 0; r < iRecords; r++)
                {
                    int iCodeSize = text.GetInt();
                    output.WriteLine("code {0}", iCodeSize);

                    for (int i = 0; i < iCodeSize; i++)
                    {
                        // Read opcode as ulong to correctly handle the full 64-bit
                        // values used by the Taleworlds classic engine. The opcode
                        // itself fits in an int, but parameters may be large unsigned
                        // 64-bit numbers (e.g. item flags, face codes).
                        ulong iOpCode = text.GetUInt64();
                        int iOpCodeInt = (int)(iOpCode & 0xFFFFFFFF);

                        if (!operators.TryGetValue(iOpCodeInt, out var op))
                        {
                            output.WriteLine("\tunknown_opcode_{0}", iOpCodeInt);
                            continue;
                        }

                        output.Write("\t{0}", op.Value);
                        int iNumParams = op.Parameters.Count;

                        for (int p = 0; p < iNumParams; p++)
                        {
                            // Parameters must be read as ulong to avoid truncating
                            // large values such as item capability flags or face codes.
                            ulong iParam = text.GetUInt64();
                            output.Write(" {0}", op.GetParameter(p, iParam.ToString()));
                        }
                        output.WriteLine();
                    }
                }
            }
        }
    }
}
