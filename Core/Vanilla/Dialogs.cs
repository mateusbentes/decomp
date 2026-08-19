using System.Text;
using DWORD = System.UInt32;

namespace Decomp.Core.Vanilla;

public static class Dialogs
{
    public static void Decompile()
    {
        var inputPath = Path.Combine(Common.InputPath, "conversation.txt");
        var outputPath = Path.Combine(Common.OutputPath, "module_dialogs.py");
        using var dialogs = new Text(inputPath);
        using var source = new FileWriter(outputPath);

        source.WriteLine(Header.Standard);
        source.WriteLine(Header.Dialogs);
        dialogs.GetString();
        var dialogCount = dialogs.GetInt();

        for (var index = 0; index < dialogCount; index++)
        {
            dialogs.GetWord();
            var dialogPartner = dialogs.GetUInt();
            var startingDialogState = dialogs.GetInt();
            var partnerText = new StringBuilder(256);
            string[] repeatPrefixes = [
                "repeat_for_factions",
                "repeat_for_parties",
                "repeat_for_troops",
                "repeat_for_100",
                "repeat_for_1000",
            ];
            var repeat = (dialogPartner & 0x00007000) >> 12;
            if (repeat != 0 && repeat <= repeatPrefixes.Length)
                partnerText.Append(repeatPrefixes[repeat - 1]).Append('|');

            string[] partnerPrefixes = ["plyr", "party_tpl", "auto_proceed", "multi_line"];
            uint[] partnerFlags = [0x00010000, 0x00020000, 0x00040000, 0x00080000];
            for (var flagIndex = 0; flagIndex < partnerFlags.Length; flagIndex++)
            {
                if ((partnerFlags[flagIndex] & dialogPartner) != 0)
                    partnerText.Append(partnerPrefixes[flagIndex]).Append('|');
            }

            const DWORD PartyTemplateFlag = 0x00020000;
            var partner = dialogPartner & 0x00000FFF;
            if (partner == 0x00000FFF)
            {
                partnerText.Append("anyone|");
            }
            else if (partner != 0)
            {
                if ((dialogPartner & PartyTemplateFlag) != 0)
                {
                    partnerText.Append(partner < Common.PTemps.Count
                        ? $"pt_{Common.PTemps[(int)partner]}|"
                        : $"{partner}|");
                }
                else
                {
                    partnerText.Append(partner < Common.Troops.Count
                        ? $"trp_{Common.Troops[(int)partner]}|"
                        : $"{partner}|");
                }
            }

            var other = (dialogPartner & 0xFFF00000) >> 20;
            if (other != 0)
            {
                partnerText.Append(other < Common.Troops.Count
                    ? $"other(trp_{Common.Troops[(int)other]})|"
                    : $"other({other})|");
            }

            if (partnerText.Length == 0)
                partnerText.Append('0');
            else
                partnerText.Length--;

            if (startingDialogState < Common.DialogStates.Count)
                source.Write("  [{0}, \"{1}\",\r\n    [", partnerText, Common.DialogStates[startingDialogState]);
            else
                source.Write("  [{0}, {1},\r\n    [", partnerText, startingDialogState);

            var recordCount = dialogs.GetInt();
            if (recordCount != 0)
            {
                source.WriteLine();
                Common.PrintStatement(dialogs, source, recordCount, "      ");
                source.WriteLine("    ],");
            }
            else
            {
                source.WriteLine("],");
            }

            source.WriteLine("    \"{0}\",", dialogs.GetWord().Replace('_', ' '));
            var endingDialogState = dialogs.GetInt();
            if (endingDialogState < Common.DialogStates.Count)
                source.Write("    \"{0}\",\r\n    [", Common.DialogStates[endingDialogState]);
            else
                source.Write("    {0},\r\n    [", endingDialogState);

            recordCount = dialogs.GetInt();
            if (recordCount != 0)
            {
                source.WriteLine();
                Common.PrintStatement(dialogs, source, recordCount, "      ");
                source.Write("    ]");
            }
            else
            {
                source.Write(']');
            }

            source.WriteLine("],\r\n");
        }

        source.Write(']');
    }
}
