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
        private static string GetDirectory(this DirectoryNotFoundException exception)
        {
            var message = exception.Message;
            var beginPos = message.IndexOf('"');
            var endPos = message.IndexOf('"', beginPos + 1);
            return Path.GetDirectoryName(message.Substring(beginPos + 1, endPos - beginPos - 1));
        }

        private static string _status = string.Empty;
        public static string Status
        {
            get => _status;
            set
            {
                _status = value;
                Console.WriteLine(value);
            }
        }

        private static Thread _workThread = new Thread(Decompile);

        public static bool Alive => _workThread.IsAlive;

        public static void StopDecompilation() => _workThread.Abort();

        public static void StartDecompilation()
        {
            _workThread = new Thread(Decompile);
            _workThread.Start();
        }

        public static void Decompile()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(1033);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(1033);

            var sw = Stopwatch.StartNew();
            Console.WriteLine(Application.GetResource("LocalizationInitialization") + " ");

            Status = "";

            InitializePath(out var isSingleFile);

            if (!File.Exists(Common.InputPath) && !Directory.Exists(Common.InputPath))
            {
                Console.WriteLine("\n" + Application.GetResource("LocalizationPleasePath"));
                Status = "";
                return;
            }

            if (!Directory.Exists(Common.OutputPath))
            {
                try
                {
                    Directory.CreateDirectory(Common.OutputPath);
                }
                catch
                {
                    Console.WriteLine("\n" + Application.GetResource("LocalizationPleaseOutput"));
                    Status = "";
                    return;
                }
            }

            try
            {
                if (!isSingleFile)
                {
                    InitializeOpCodes();
                    InitializeModuleData();
                    Common.NeedId = true; // Default to true, can be changed if needed
                }
                else
                {
                    var f = InitializeTrie[GetSingleFileName()] ?? (() => { });
                    f();
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("\n" + Application.GetResource("LocalizationFileNotFound"), ex.FileName);
                Status = "";
                return;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("\n" + Application.GetResource("LocalizationDirectoryNotFound"), ex.GetDirectory());
                Status = "";
                return;
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine(Application.GetResource("LocalizationDecompilationCanceled") + "\n");
                Status = "";
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{Application.GetResource("LocalizationFatalErrorDecompilationAborted")}\n");
                Console.WriteLine("{0}\n", e.Message);
                Console.WriteLine("{0}\n", e.StackTrace);
                Status = "";
                return;
            }

            Console.WriteLine(Application.GetResource("LocalizationTime") + "\n", sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency);

            var success = false;

#if RELEASE
            try
            {
                if (isSingleFile)
                    ProcessSingleFile();
                else
                    ProcessFullModule();
                success = true;
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine(Application.GetResource("LocalizationDecompilationCanceled") + "\n");
                Status = "";
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(Application.GetResource("LocalizationFatalErrorDecompilationAborted") + "\n");
                Console.WriteLine("{0}\n", ex.Message);
                Console.WriteLine("{0}\n", ex.StackTrace);
            }
#endif

            Console.WriteLine(Application.GetResource("LocalizationTotalTime") + "\n", sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency);

            if (success) Process.Start(Common.OutputPath);
        }

        private static void InitializePath(out bool isSingleFile)
        {
            var x = false;
            if (!File.Exists(Common.InputPath))
            {
                Common.InputPath = Common.InputPath;
                x = false;
            }
            else
            {
                Common.InputPath = Path.GetDirectoryName(Common.InputPath);
                x = true;
            }
            Common.OutputPath = Common.OutputPath;

            if (Common.InputPath != null && Common.InputPath[Common.InputPath.Length - 1] == '\\')
                Common.InputPath = Common.InputPath.Remove(Common.InputPath.Length - 1, 1);
            if (Common.OutputPath[Common.OutputPath.Length - 1] == '\\')
                Common.OutputPath = Common.OutputPath.Remove(Common.OutputPath.Length - 1, 1);
            isSingleFile = x;
        }

        private static void InitializeOpCodes()
        {
            var opCodes = new Dictionary<int, Operator>();
            Common.SelectedMode = Mode.Vanilla; // Default mode, can be changed if needed
            var operators = Operator.GetCollection(Common.SelectedMode);
            foreach (var op in operators) opCodes[op.Code] = op;
            Common.Operators = opCodes;
        }

        private static void InitializeModuleData()
        {
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} scripts.txt";
            Common.Procedures = Scripts.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} quick_strings.txt";
            Common.QuickStrings = QuickStrings.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} strings.txt";
            Common.Strings = Strings.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} item_kinds1.txt";
            Common.Items = Text.GetFirstStringFromFile(Common.InputPath + @"\item_kinds1.txt") == "itemsfile version 2"
                ? Vanilla.Items.GetIdFromFile(Common.InputPath + @"\item_kinds1.txt") : Items.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} troops.txt";
            Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + @"\troops.txt") == "troopsfile version 1"
                ? Vanilla.Troops.GetIdFromFile(Common.InputPath + @"\troops.txt") : (Common.SelectedMode == Mode.Caribbean ?
                    Caribbean.Troops.Initialize() : Troops.Initialize());
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} factions.txt";
            Common.Factions = Factions.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} quests.txt";
            Common.Quests = Quests.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} party_templates.txt";
            Common.PTemps = PartyTemplates.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} parties.txt";
            Common.Parties = Parties.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} menus.txt";
            Common.Menus = Menus.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} sounds.txt";
            Common.Sounds = Sounds.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} skills.txt";
            Common.Skills = Skills.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} meshes.txt";
            Common.Meshes = Meshes.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} variables.txt";
            Common.Variables = Scripts.InitializeVariables();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} dialog_states.txt";
            Common.DialogStates = Dialogs.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} scenes.txt";
            Common.Scenes = Scenes.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} mission_templates.txt";
            Common.MissionTemplates = MissionTemplates.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} particle_systems.txt";
            Common.ParticleSystems = ParticleSystems.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} scene_props.txt";
            Common.SceneProps = SceneProps.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} map_icons.txt";
            Common.MapIcons = MapIcons.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} presentations.txt";
            Common.Presentations = Presentations.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} tableau_materials.txt";
            Common.Tableaus = TableauMaterials.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} actions.txt";
            Common.Animations = Common.IsVanillaMode ? Vanilla.Animations.GetIdFromFile(Common.InputPath + @"\actions.txt") : Animations.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} music.txt";
            Common.Music = Music.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} skins.txt";
            Common.Skins = Common.SelectedMode == Mode.Caribbean ?
                Caribbean.Skins.Initialize() : Skins.Initialize();
            Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} info_pages.txt";
            Common.InfoPages = InfoPages.Initialize();
            Status = Application.GetResource("LocalizationDecompilation");
        }

        private static void ProcessFile(string strFileName)
        {
            if (!File.Exists(Path.Combine(Common.InputPath, strFileName)))
            {
                Console.WriteLine(Application.GetResource("LocalizationFileNotFound2") + "\n", Common.InputPath, strFileName);
                return;
            }

            var sw = Stopwatch.StartNew();
            var dblTime = sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

            var fInput = new Text(Path.Combine(Common.InputPath, strFileName));
            var strFirstString = fInput.GetString();
            if (strFirstString == null)
            {
                Console.WriteLine(Application.GetResource("LocalizationUnknownFormat") + "\n");
                return;
            }
            var bFirstNumber = Int32.TryParse(strFirstString, out var _);
            fInput.Close();

            if (strFirstString == "scriptsfile version 1")
                Scripts.Decompile();
            else if (strFirstString == "triggersfile version 1")
                Triggers.Decompile();
            else if (strFirstString == "simple_triggers_file version 1")
                SimpleTriggers.Decompile();
            else if (strFirstString == "dialogsfile version 2")
                Dialogs.Decompile();
            else if (strFirstString == "dialogsfile version 1")
                Vanilla.Dialogs.Decompile();
            else if (strFirstString == "menusfile version 1")
                Menus.Decompile();
            else if (strFirstString == "factionsfile version 1")
                Factions.Decompile();
            else if (strFirstString == "infopagesfile version 1")
                InfoPages.Decompile();
            else if (strFirstString == "itemsfile version 3")
                Items.Decompile();
            else if (strFirstString == "itemsfile version 2")
                Vanilla.Items.Decompile();
            else if (strFirstString == "map_icons_file version 1")
                MapIcons.Decompile();
            else if (strFirstString == "missionsfile version 1")
                MissionTemplates.Decompile();
            else if (strFirstString == "particle_systemsfile version 1")
                ParticleSystems.Decompile();
            else if (strFirstString == "partiesfile version 1")
                Parties.Decompile();
            else if (strFirstString == "partytemplatesfile version 1")
                PartyTemplates.Decompile();
            else if (strFirstString == "postfx_paramsfile version 1")
                Postfx.Decompile();
            else if (strFirstString == "presentationsfile version 1")
                Presentations.Decompile();
            else if (strFirstString == "questsfile version 1")
                Quests.Decompile();
            else if (strFirstString == "scene_propsfile version 1")
                SceneProps.Decompile();
            else if (strFirstString == "scenesfile version 1")
                Scenes.Decompile();
            else if (strFirstString == "skins_file version 1" && Common.SelectedMode == Mode.Caribbean)
                Caribbean.Skins.Decompile();
            else if (strFirstString == "skins_file version 1")
                Skins.Decompile();
            else if (strFirstString == "soundsfile version 3")
                Sounds.Decompile();
            else if (strFirstString == "soundsfile version 2")
                Vanilla.Sounds.Decompile();
            else if (strFirstString == "stringsfile version 1")
                Strings.Decompile();
            else if (strFirstString == "troopsfile version 2" && Common.SelectedMode == Mode.Caribbean)
                Caribbean.Troops.Decompile();
            else if (strFirstString == "troopsfile version 2")
                Troops.Decompile();
            else if (strFirstString == "troopsfile version 1")
                Vanilla.Troops.Decompile();
            else if (bFirstNumber && strFileName == "tableau_materials.txt")
                TableauMaterials.Decompile();
            else if (bFirstNumber && strFileName == "skills.txt")
                Skills.Decompile();
            else if (bFirstNumber && strFileName == "music.txt")
                Music.Decompile();
            else if (bFirstNumber && strFileName == "actions.txt")
            {
                if (Common.IsVanillaMode)
                    Vanilla.Animations.Decompile();
                else
                    Animations.Decompile();
            }
            else if (bFirstNumber && strFileName == "meshes.txt")
                Meshes.Decompile();
            else if (bFirstNumber && strFileName == "flora_kinds.txt")
                Flora.Decompile();
            else if (strFileName == "ground_specs.txt")
                GroundSpecs.Decompile();
            else if (bFirstNumber && strFileName == "skyboxes.txt")
                Skyboxes.Decompile();
            else
                Console.WriteLine(Application.GetResource("LocalizationUnknownFormat") + "\n");

            Console.WriteLine(Application.GetResource("LocalizationFileTime") + "\n", strFileName, sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency - dblTime);
        }

        private static string GetSingleFileName()
        {
            return Path.GetFileName(Common.InputPath);
        }

        public static string GetShadersFullFileName(out bool founded)
        {
            founded = true;
            if (File.Exists(Path.Combine(Common.InputPath, "mb_2a.fxo"))) return Path.Combine(Common.InputPath, "mb_2a.fxo");
            if (File.Exists(Path.Combine(Common.InputPath, "mb_2b.fxo"))) return Path.Combine(Common.InputPath, "mb_2b.fxo");
            if (File.Exists(Path.Combine(Common.InputPath, "mb.fx"))) return Path.Combine(Common.InputPath, "mb.fx");
            founded = false;
            return "";
        }

        private static void ProcessSingleFile()
        {
            var strFileName = GetSingleFileName();
            Common.NeedId = false;

            var ext = Path.GetExtension(strFileName);
            if (ext == ".fx" || ext == ".fxo")
            {
                var sw = Stopwatch.StartNew();
                var dblTime = sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                Shaders.Shaders.Decompile(Path.Combine(Common.InputPath, strFileName));
                Console.WriteLine(Application.GetResource("LocalizationFileTime") + "\n", strFileName, sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency - dblTime);
                return;
            }

            if (ext == ".brf")
            {
                var sw = Stopwatch.StartNew();
                var dblTime = sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                switch (strFileName)
                {
                    case "core_shaders.brf":
                        WSE2.Shaders.Decompile();
                        break;
                    case "core_physics_materials.brf":
                        WSE2.PhysicsMaterials.Decompile();
                        break;
                }
                Console.WriteLine(Application.GetResource("LocalizationFileTime") + "\n", strFileName, sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency - dblTime);
                return;
            }

            string[] strModFiles = { "actions.txt", "conversation.txt", "factions.txt", "info_pages.txt", "item_kinds1.txt", "map_icons.txt",
            "menus.txt", "meshes.txt", "mission_templates.txt", "music.txt", "particle_systems.txt", "parties.txt", "party_templates.txt",
            "postfx.txt", "presentations.txt", "quests.txt", "scene_props.txt", "scenes.txt", "scripts.txt", "simple_triggers.txt",
            "skills.txt", "skins.txt", "sounds.txt", "strings.txt", "tableau_materials.txt", "triggers.txt", "troops.txt",
            "flora_kinds.txt", "ground_specs.txt", "skyboxes.txt" };

            var strFileToProcess = strModFiles.FirstOrDefault(t => t == strFileName);
            ProcessFile(strFileToProcess);
        }

        private static void ProcessFullModule()
        {
            File.Copy(Path.Combine(Common.InputPath, "variables.txt"), Path.Combine(Common.OutputPath, "variables.txt"), true);

            var decompileShaders = true; // Default to true, can be changed if needed

            if (!Common.IsVanillaMode)
                Win32FileWriter.WriteAllText(Path.Combine(Common.OutputPath, "module_constants.py"), Header.Standard + Common.ModuleConstantsText);
            else
                Win32FileWriter.WriteAllText(Path.Combine(Common.OutputPath, "module_constants.py"), Header.Standard + Common.ModuleConstantsVanillaText);

            string[] strModFiles = { "actions.txt", "conversation.txt", "factions.txt", "info_pages.txt", "item_kinds1.txt", "map_icons.txt",
            "menus.txt", "meshes.txt", "mission_templates.txt", "music.txt", "particle_systems.txt", "parties.txt", "party_templates.txt",
            "postfx.txt", "presentations.txt", "quests.txt", "scene_props.txt", "scenes.txt", "scripts.txt", "simple_triggers.txt",
            "skills.txt", "skins.txt", "sounds.txt", "strings.txt", "tableau_materials.txt", "triggers.txt", "troops.txt" };
            string[] strModDataFiles = { "flora_kinds.txt", "ground_specs.txt", "skyboxes.txt" };

            int iNumFiles = strModFiles.Length;
            if (Common.IsVanillaMode) iNumFiles -= 2;

            iNumFiles += strModDataFiles.Count(strModDataFile => File.Exists(Path.Combine(Common.InputPath, "Data", strModDataFile)));

            var sShadersFile = GetShadersFullFileName(out var b);
            if (b && decompileShaders) iNumFiles++;

            double dblProgressForOneFile = 100.0 / iNumFiles, dblProgress = 0;

            foreach (var strModFile in strModFiles.Where(strModFile => !(Common.IsVanillaMode && (strModFile == "info_pages.txt" || strModFile == "postfx.txt"))))
            {
                ProcessFile(strModFile);
                dblProgress += dblProgressForOneFile;
                Status = $"{Application.GetResource("LocalizationDecompilation")} {dblProgress:F2}%";
            }

            if (b && decompileShaders)
            {
                ProcessShaders(sShadersFile);
                dblProgress += dblProgressForOneFile;
                Status = $"{Application.GetResource("LocalizationDecompilation")} {dblProgress:F2}%";
            }

            Common.InputPath = Path.Combine(Common.InputPath, "Data");

            foreach (var strModDataFile in strModDataFiles.Where(strModDataFile => File.Exists(Path.Combine(Common.InputPath, strModDataFile))))
            {
                ProcessFile(strModDataFile);
                dblProgress += dblProgressForOneFile;
                Status = $"{Application.GetResource("LocalizationDecompilation")} {dblProgress:F2}%";
            }
        }

        private static void ProcessShaders(string sShadersFile)
        {
            var sw = Stopwatch.StartNew();
            Shaders.Shaders.Decompile(sShadersFile);
            Console.WriteLine(Application.GetResource("LocalizationFileTime") + "\n", Path.GetFileName(sShadersFile), sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency);
        }

        private static readonly SimpleTrie<Action> InitializeTrie = new SimpleTrie<Action>
        {
            ["actions.txt"] = () => { },
            ["conversation.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["factions.txt"] = () => { },
            ["info_pages.txt"] = () => { },
            ["item_kinds1.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["map_icons.txt"] = () => { },
            ["menus.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["meshes.txt"] = () => { },
            ["mission_templates.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["music.txt"] = () => { },
            ["particle_systems.txt"] = () => { },
            ["parties.txt"] = () => {
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} troops.txt";
                Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + @"\troops.txt") == "troopsfile version 1"
                    ? Vanilla.Troops.GetIdFromFile(Common.InputPath + @"\troops.txt") : Troops.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} factions.txt";
                Common.Factions = Factions.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} map_icons.txt";
                Common.MapIcons = MapIcons.Initialize();
                Status = Application.GetResource("LocalizationDecompilation");
            },
            ["party_templates.txt"] = () => {
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} troops.txt";
                Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + @"\troops.txt") == "troopsfile version 1"
                    ? Vanilla.Troops.GetIdFromFile(Common.InputPath + @"\troops.txt") : Troops.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} factions.txt";
                Common.Factions = Factions.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} map_icons.txt";
                Common.MapIcons = MapIcons.Initialize();
                Status = Application.GetResource("LocalizationDecompilation");
            },
            ["postfx.txt"] = () => { },
            ["presentations.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["quests.txt"] = () => { },
            ["scene_props.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["scenes.txt"] = () => { },
            ["scripts.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["simple_triggers.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["skills.txt"] = () => { },
            ["skins.txt"] = () => { },
            ["sounds.txt"] = () => { },
            ["strings.txt"] = () => { },
            ["tableau_materials.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["triggers.txt"] = () => { InitializeOpCodes(); InitializeModuleData(); },
            ["troops.txt"] = () => {
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} item_kinds1.txt";
                Common.Items = Text.GetFirstStringFromFile(Common.InputPath + @"\item_kinds1.txt") == "itemsfile version 2"
                    ? Vanilla.Items.GetIdFromFile(Common.InputPath + @"\item_kinds1.txt") : Items.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} scenes.txt";
                Common.Scenes = Scenes.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} skins.txt";
                Common.Skins = Skins.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} factions.txt";
                Common.Factions = Factions.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} skills.txt";
                Common.Skills = Skills.Initialize();
                Status = $"{Application.GetResource("LocalizationDecompilation")} -- {Application.GetResource("LocalizationInitialization")} troops.txt";
                Common.Troops = Text.GetFirstStringFromFile(Common.InputPath + @"\troops.txt") == "troopsfile version 1"
                    ? Vanilla.Troops.GetIdFromFile(Common.InputPath + @"\troops.txt") : Troops.Initialize();
                Status = Application.GetResource("LocalizationDecompilation");
            }
        };
    }
}
