#define RELEASE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Decomp.Core.Operators;

namespace Decomp.Core
{
    public static class Decompiler
    {
        public static void Decompile(string inputFile, string outputFile = null, string gameVersion = "VanillaWarband")
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputFile);
            }

            // Mapear VanillaWFS para VanillaWarband para usar os mesmos operadores
            if (gameVersion.Equals("VanillaWFS", StringComparison.OrdinalIgnoreCase))
            {
                gameVersion = "VanillaWarband";
            }

            // Carregar o operador correto baseado na versão do jogo
            IGameVersion version = gameVersion.ToLowerInvariant() switch
            {
                "vanillaclassic" => new VanillaVersion(),
                "vanillawarband" => new Warband1153Version(),
                "wse320" => new WarbandScriptEnhancer320Version(),
                "wse450" => new WarbandScriptEnhancer450Version(),
                "caribbean" => new CaribbeanVersion(),
                _ => throw new ArgumentException($"Versão do jogo não suportada: {gameVersion}")
            };

            Operator op = new Operator();
            op.SetVersion(version);

            // Processar o arquivo de entrada
            Text text = new Text(inputFile);

            Win32FileWriter output = null;
            try
            {
                output = string.IsNullOrEmpty(outputFile)
                    ? new Win32FileWriter(Console.OpenStandardOutput())
                    : new Win32FileWriter(outputFile);

                // Lógica real de descompilação
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
                            string sOpCode = op.GetOperationName(iOpCode);
                            int iNumParams = op.GetNumParams(iOpCode);
                            output.Write("\t{0}", sOpCode);

                            for (int p = 0; p < iNumParams; p++)
                            {
                                int iParam = text.GetInt();
                                output.Write(" {0}", op.GetParameter(i, sOpCode, iParam));
                            }
                            output.WriteLine();
                        }
                    }
                }
            }
            finally
            {
                output?.Close();
            }
        }
    }
}
