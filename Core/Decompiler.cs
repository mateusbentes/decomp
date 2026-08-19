using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Decomp.Core.Operators;
using Decomp.Core.Shaders;

namespace Decomp.Core;

public static class Decompiler
{
    private static readonly SemaphoreSlim DecompilationGate = new(1, 1);

    private static readonly string[] ModuleFiles =
    [
        "actions.txt",
        "conversation.txt",
        "factions.txt",
        "info_pages.txt",
        "item_kinds1.txt",
        "map_icons.txt",
        "menus.txt",
        "meshes.txt",
        "mission_templates.txt",
        "music.txt",
        "particle_systems.txt",
        "parties.txt",
        "party_templates.txt",
        "postfx.txt",
        "presentations.txt",
        "quests.txt",
        "scene_props.txt",
        "scenes.txt",
        "scripts.txt",
        "simple_triggers.txt",
        "skills.txt",
        "skins.txt",
        "sounds.txt",
        "strings.txt",
        "tableau_materials.txt",
        "triggers.txt",
        "troops.txt",
    ];

    private static readonly string[] DataFiles =
    [
        "flora_kinds.txt",
        "ground_specs.txt",
        "skyboxes.txt",
    ];

    private static readonly string[] ShaderExtensions = [".vsh", ".psh", ".fxc", ".glsl"];

    public static event Action<string>? LogMessage;

    public static event Action<DecompilationProgress>? ProgressChanged;

    /// <summary>
    /// Keeps the synchronous API available for existing integrations.
    /// New callers should prefer <see cref="DecompileAsync"/>.
    /// </summary>
    public static void Decompile(
        string inputPath,
        string outputPath,
        string version = "VanillaWarband")
    {
        DecompileAsync(inputPath, outputPath, version).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Decompiles a module directory, module text file, or supported shader asynchronously.
    /// </summary>
    public static async Task DecompileAsync(
        string inputPath,
        string outputPath,
        string version = "VanillaWarband",
        IProgress<DecompilationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var normalizedInput = NormalizePath(inputPath);
        var normalizedOutput = NormalizePath(outputPath);

        await DecompilationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(normalizedInput) && !Directory.Exists(normalizedInput))
            {
                throw new FileNotFoundException("The input path does not exist.", normalizedInput);
            }

            if (File.Exists(normalizedInput))
            {
                await DecompileSingleFileAsync(
                    normalizedInput,
                    normalizedOutput,
                    version,
                    progress,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DecompileModuleDirectoryAsync(
                    normalizedInput,
                    normalizedOutput,
                    version,
                    progress,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            DecompilationGate.Release();
        }
    }

    public static string NormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = path.Trim();
        if (normalized == "~")
        {
            normalized = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else if (normalized.StartsWith("~/", StringComparison.Ordinal) ||
                 normalized.StartsWith("~\\", StringComparison.Ordinal))
        {
            normalized = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                normalized[2..]);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            normalized = normalized.Replace('\\', Path.DirectorySeparatorChar);
        }
        else
        {
            normalized = normalized.Replace('/', Path.DirectorySeparatorChar);
        }

        return Path.GetFullPath(normalized);
    }

    private static async Task DecompileSingleFileAsync(
        string inputFile,
        string outputPath,
        string version,
        IProgress<DecompilationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(inputFile);
        if (ShaderExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var shaderOutput = ResolveShaderOutputPath(inputFile, outputPath);
            await ShaderDecompiler.DecompileAsync(
                inputFile,
                shaderOutput,
                version,
                cancellationToken).ConfigureAwait(false);

            Report(progress, $"Shader written to {shaderOutput}", 1, 1, true);
            return;
        }

        if (!extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Unsupported input extension '{extension}'. Supported module and shader formats are .txt, .vsh, .psh, .fxc, and .glsl.");
        }

        var inputDirectory = Path.GetDirectoryName(inputFile) ?? Directory.GetCurrentDirectory();
        var outputDirectory = ResolveModuleOutputDirectory(outputPath);
        await EnsureDirectoryAsync(outputDirectory, cancellationToken).ConfigureAwait(false);
        await InitializeRuntimeAsync(inputDirectory, outputDirectory, version, cancellationToken).ConfigureAwait(false);

        Report(progress, $"Processing {Path.GetFileName(inputFile)}", 0, 1, false);
        await ProcessModuleFileAsync(inputFile, cancellationToken).ConfigureAwait(false);
        Report(progress, $"Completed {Path.GetFileName(inputFile)}", 1, 1, true);
    }

    private static async Task DecompileModuleDirectoryAsync(
        string inputDirectory,
        string outputDirectory,
        string version,
        IProgress<DecompilationProgress>? progress,
        CancellationToken cancellationToken)
    {
        await EnsureDirectoryAsync(outputDirectory, cancellationToken).ConfigureAwait(false);
        await InitializeRuntimeAsync(inputDirectory, outputDirectory, version, cancellationToken).ConfigureAwait(false);

        var moduleFiles = ModuleFiles
            .Select(fileName => Path.Combine(inputDirectory, fileName))
            .Where(File.Exists)
            .ToList();
        var dataFiles = DataFiles
            .Select(fileName => Path.Combine(inputDirectory, "Data", fileName))
            .Where(File.Exists)
            .ToList();
        var shaderFile = FindShaderFile(inputDirectory);
        var totalFiles = moduleFiles.Count + dataFiles.Count + (shaderFile is null ? 0 : 1);
        var processedFiles = 0;

        if (File.Exists(Path.Combine(inputDirectory, "variables.txt")))
        {
            await CopyFileAsync(
                Path.Combine(inputDirectory, "variables.txt"),
                Path.Combine(outputDirectory, "variables.txt"),
                cancellationToken).ConfigureAwait(false);
        }

        var constants = Common.IsVanillaMode
            ? Common.ModuleConstantsVanillaText
            : Common.ModuleConstantsText;
        await FileWriter.WriteAllTextAsync(
            Path.Combine(outputDirectory, "module_constants.py"),
            Header.Standard + constants,
            cancellationToken).ConfigureAwait(false);

        foreach (var moduleFile in moduleFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessModuleFileAsync(moduleFile, cancellationToken).ConfigureAwait(false);
            processedFiles++;
            Report(progress, $"Processed {Path.GetFileName(moduleFile)}", processedFiles, totalFiles, false);
        }

        if (shaderFile is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var shaderOutput = Path.Combine(
                outputDirectory,
                $"{Path.GetFileNameWithoutExtension(shaderFile)}.txt");
            await ShaderDecompiler.DecompileAsync(
                shaderFile,
                shaderOutput,
                version,
                cancellationToken).ConfigureAwait(false);
            processedFiles++;
            Report(progress, $"Processed {Path.GetFileName(shaderFile)}", processedFiles, totalFiles, false);
        }

        if (dataFiles.Count > 0)
        {
            Common.InputPath = Path.Combine(inputDirectory, "Data");
            foreach (var dataFile in dataFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessModuleFileAsync(dataFile, cancellationToken).ConfigureAwait(false);
                processedFiles++;
                Report(progress, $"Processed {Path.GetFileName(dataFile)}", processedFiles, totalFiles, false);
            }
            Common.InputPath = inputDirectory;
        }

        Report(progress, "Decompilation completed successfully.", processedFiles, totalFiles, true);
    }

    private static async Task InitializeRuntimeAsync(
        string inputDirectory,
        string outputDirectory,
        string version,
        CancellationToken cancellationToken)
    {
        await Task.Run(
            () =>
            {
                Common.InputPath = inputDirectory;
                Common.OutputPath = outputDirectory;
                var gameVersion = ResolveGameVersion(version);
                Common.Operators = gameVersion
                    .GetOperators()
                    .GroupBy(operatorDefinition => operatorDefinition.Code)
                    .ToDictionary(group => group.Key, group => group.First());
                InitializeModuleData();
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task ProcessModuleFileAsync(
        string inputFile,
        CancellationToken cancellationToken)
    {
        var firstLine = await Text.GetFirstLineFromFileAsync(inputFile, cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        await Task.Run(
            () => DispatchModuleFile(inputFile, firstLine),
            cancellationToken).ConfigureAwait(false);
    }

    private static void DispatchModuleFile(string inputFile, string firstLine)
    {
        var fileName = Path.GetFileName(inputFile);
        var isFirstNumber = int.TryParse(
            firstLine,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out _);

        switch (firstLine)
        {
            case "scriptsfile version 1":
                Scripts.Decompile();
                break;
            case "triggersfile version 1":
                Triggers.Decompile();
                break;
            case "simple_triggers_file version 1":
                SimpleTriggers.Decompile();
                break;
            case "dialogsfile version 2":
                Dialogs.Decompile();
                break;
            case "dialogsfile version 1":
                Vanilla.Dialogs.Decompile();
                break;
            case "menusfile version 1":
                Menus.Decompile();
                break;
            case "factionsfile version 1":
                Factions.Decompile();
                break;
            case "infopagesfile version 1":
                InfoPages.Decompile();
                break;
            case "itemsfile version 3":
                Items.Decompile();
                break;
            case "itemsfile version 2":
                Vanilla.Items.Decompile();
                break;
            case "map_icons_file version 1":
                MapIcons.Decompile();
                break;
            case "missionsfile version 1":
                MissionTemplates.Decompile();
                break;
            case "particle_systemsfile version 1":
                ParticleSystems.Decompile();
                break;
            case "partiesfile version 1":
                Parties.Decompile();
                break;
            case "partytemplatesfile version 1":
                PartyTemplates.Decompile();
                break;
            case "postfx_paramsfile version 1":
                Postfx.Decompile();
                break;
            case "presentationsfile version 1":
                Presentations.Decompile();
                break;
            case "questsfile version 1":
                Quests.Decompile();
                break;
            case "scene_propsfile version 1":
                SceneProps.Decompile();
                break;
            case "scenesfile version 1":
                Scenes.Decompile();
                break;
            case "skins_file version 1":
                if (Common.SelectedMode == GameMode.Caribbean)
                    Caribbean.Skins.Decompile();
                else
                    Skins.Decompile();
                break;
            case "soundsfile version 3":
                Sounds.Decompile();
                break;
            case "soundsfile version 2":
                Vanilla.Sounds.Decompile();
                break;
            case "stringsfile version 1":
                Strings.Decompile();
                break;
            case "troopsfile version 2":
                if (Common.SelectedMode == GameMode.Caribbean)
                    Caribbean.Troops.Decompile();
                else
                    Troops.Decompile();
                break;
            case "troopsfile version 1":
                Vanilla.Troops.Decompile();
                break;
            default:
                DispatchNumberedFile(fileName, isFirstNumber);
                break;
        }
    }

    private static void DispatchNumberedFile(string fileName, bool isFirstNumber)
    {
        if (!isFirstNumber)
        {
            throw new NotSupportedException(
                $"Unknown module file format in '{fileName}'. The first line was not a recognized TaleWorlds header.");
        }

        switch (fileName.ToLowerInvariant())
        {
            case "tableau_materials.txt":
                TableauMaterials.Decompile();
                break;
            case "skills.txt":
                Skills.Decompile();
                break;
            case "music.txt":
                Music.Decompile();
                break;
            case "actions.txt":
                if (Common.IsVanillaMode)
                    Vanilla.Animations.Decompile();
                else
                    Animations.Decompile();
                break;
            case "meshes.txt":
                Meshes.Decompile();
                break;
            case "flora_kinds.txt":
                Flora.Decompile();
                break;
            case "ground_specs.txt":
                GroundSpecs.Decompile();
                break;
            case "skyboxes.txt":
                Skyboxes.Decompile();
                break;
            default:
                throw new NotSupportedException($"Unknown numbered module file '{fileName}'.");
        }
    }

    private static void InitializeModuleData()
    {
        Common.Procedures = Scripts.Initialize();
        Common.QuickStrings = QuickStrings.Initialize();
        Common.Strings = Strings.Initialize();

        var itemKindsPath = Path.Combine(Common.InputPath, "item_kinds1.txt");
        Common.Items = ReadHeader(itemKindsPath) == "itemsfile version 2"
            ? Vanilla.Items.GetIdFromFile(itemKindsPath)
            : Items.Initialize();

        var troopsPath = Path.Combine(Common.InputPath, "troops.txt");
        Common.Troops = ReadHeader(troopsPath) == "troopsfile version 1"
            ? Vanilla.Troops.GetIdFromFile(troopsPath)
            : Common.SelectedMode == GameMode.Caribbean
                ? Caribbean.Troops.Initialize()
                : Troops.Initialize();

        Common.Factions = Factions.Initialize();
        Common.Quests = Quests.Initialize();
        Common.PTemps = PartyTemplates.Initialize();
        Common.PartyTemplates = Common.PTemps;
        Common.Parties = Parties.Initialize();
        Common.Menus = Menus.Initialize();
        Common.Sounds = Sounds.Initialize();
        Common.Skills = Skills.Initialize();
        Common.Meshes = Meshes.Initialize();
        Common.Variables = Scripts.InitializeVariables();
        Common.DialogStates = Dialogs.Initialize();
        Common.Scenes = Scenes.Initialize();
        Common.MissionTemplates = MissionTemplates.Initialize();
        Common.ParticleSystems = ParticleSystems.Initialize();
        Common.SceneProps = SceneProps.Initialize();
        Common.MapIcons = MapIcons.Initialize();
        Common.Presentations = Presentations.Initialize();
        Common.Tableaus = TableauMaterials.Initialize();
        var actionsPath = Path.Combine(Common.InputPath, "actions.txt");
        Common.Animations = Common.IsVanillaMode && File.Exists(actionsPath)
            ? Vanilla.Animations.GetIdFromFile(actionsPath)
            : Common.IsVanillaMode
                ? Array.Empty<string>()
                : Animations.Initialize();
        Common.Music = Music.Initialize();
        Common.Skins = Common.SelectedMode == GameMode.Caribbean
            ? Caribbean.Skins.Initialize()
            : Skins.Initialize();
        Common.InfoPages = InfoPages.Initialize();
    }

    private static IGameVersion ResolveGameVersion(string version)
    {
        var normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? "vanillawarband"
            : version.Trim().ToLowerInvariant();

        return normalizedVersion switch
        {
            "vanillaclassic" or "vanilla" => SetModeAndReturn(
                GameMode.Vanilla,
                new VanillaVersion()),
            "vanillawarband" or "warband1153" or "warband" => SetModeAndReturn(
                GameMode.WarbandScriptEnhancer450,
                new Warband1153Version()),
            "warband1171" => SetModeAndReturn(
                GameMode.WarbandScriptEnhancer450,
                new Warband1171Version()),
            "vanillawfs" or "wfs" => SetModeAndReturn(
                GameMode.WarbandScriptEnhancer450,
                new WFSVersion()),
            "wse320" => SetModeAndReturn(
                GameMode.WarbandScriptEnhancer320,
                new WarbandScriptEnhancer320Version()),
            "wse450" => SetModeAndReturn(
                GameMode.WarbandScriptEnhancer450,
                new WarbandScriptEnhancer450Version()),
            "caribbean" => SetModeAndReturn(
                GameMode.Caribbean,
                new CaribbeanVersion()),
            _ => throw new ArgumentException(
                $"Unsupported game version '{version}'.",
                nameof(version)),
        };
    }

    private static IGameVersion SetModeAndReturn(GameMode mode, IGameVersion version)
    {
        Common.SelectedMode = mode;
        return version;
    }

    private static string? ReadHeader(string filePath)
    {
        return File.Exists(filePath) ? Text.GetFirstLineFromFile(filePath) : null;
    }

    private static string ResolveModuleOutputDirectory(string outputPath)
    {
        if (Directory.Exists(outputPath) || !Path.HasExtension(outputPath))
            return outputPath;

        return Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
    }

    private static string ResolveShaderOutputPath(string inputFile, string outputPath)
    {
        if (Path.HasExtension(outputPath))
            return outputPath;

        return Path.Combine(
            outputPath,
            $"{Path.GetFileNameWithoutExtension(inputFile)}.txt");
    }

    private static string? FindShaderFile(string inputDirectory)
    {
        var preferredNames = new[] { "mb_2a.fxc", "mb_2b.fxc" };
        foreach (var fileName in preferredNames)
        {
            var candidate = Path.Combine(inputDirectory, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return Directory.EnumerateFiles(inputDirectory)
            .FirstOrDefault(file => ShaderExtensions.Contains(
                Path.GetExtension(file),
                StringComparer.OrdinalIgnoreCase));
    }

    private static async Task EnsureDirectoryAsync(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Run(() => Directory.CreateDirectory(path), cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
            await EnsureDirectoryAsync(destinationDirectory, cancellationToken).ConfigureAwait(false);

        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await source.CopyToAsync(destination, 64 * 1024, cancellationToken).ConfigureAwait(false);
    }

    private static void Report(
        IProgress<DecompilationProgress>? progress,
        string message,
        int processedFiles,
        int totalFiles,
        bool isCompleted)
    {
        var update = new DecompilationProgress(message, processedFiles, totalFiles, isCompleted);
        progress?.Report(update);
        ProgressChanged?.Invoke(update);
        LogMessage?.Invoke(message);
    }
}
