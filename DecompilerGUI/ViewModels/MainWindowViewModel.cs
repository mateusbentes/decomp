using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Decomp.Core;
using DecompilerGUI.Services;
using ReactiveUI;

namespace DecompilerGUI.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject, IDisposable
{
    private readonly LocalizationService localization;
    private CancellationTokenSource? decompilationCancellation;
    private string inputPath = string.Empty;
    private string outputPath = string.Empty;
    private string selectedVersion = "VanillaWarband";
    private double progress;
    private bool isIndeterminateProgress;
    private string logOutput = string.Empty;
    private string statusMessage = string.Empty;
    private CultureInfo currentLanguage;
    private bool disposed;

    public MainWindowViewModel()
    {
        localization = new LocalizationService();
        currentLanguage = AvailableLanguages[0];

        var canExecuteOnUi = Observable.Return(true).ObserveOn(RxApp.MainThreadScheduler);
        BrowseInputCommand = ReactiveCommand.CreateFromTask(
            BrowseInputAsync,
            canExecute: canExecuteOnUi,
            outputScheduler: RxApp.MainThreadScheduler);
        BrowseOutputCommand = ReactiveCommand.CreateFromTask(
            BrowseOutputAsync,
            canExecute: canExecuteOnUi,
            outputScheduler: RxApp.MainThreadScheduler);
        DecompileCommand = ReactiveCommand.CreateFromTask(
            DecompileAsync,
            canExecute: canExecuteOnUi,
            outputScheduler: RxApp.MainThreadScheduler);

        Decompiler.LogMessage += OnLogMessageReceived;
        UpdateStatusMessage();
    }

    public string InputPath
    {
        get => inputPath;
        set => this.RaiseAndSetIfChanged(ref inputPath, NormalizePath(value));
    }

    public string OutputPath
    {
        get => outputPath;
        set => this.RaiseAndSetIfChanged(ref outputPath, NormalizePath(value));
    }

    public string SelectedVersion
    {
        get => selectedVersion;
        set => this.RaiseAndSetIfChanged(ref selectedVersion, value);
    }

    public double Progress
    {
        get => progress;
        private set => this.RaiseAndSetIfChanged(ref progress, value);
    }

    public bool IsIndeterminateProgress
    {
        get => isIndeterminateProgress;
        private set => this.RaiseAndSetIfChanged(ref isIndeterminateProgress, value);
    }

    public string LogOutput
    {
        get => logOutput;
        private set => this.RaiseAndSetIfChanged(ref logOutput, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => this.RaiseAndSetIfChanged(ref statusMessage, value);
    }

    public CultureInfo CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (Equals(currentLanguage, value))
                return;

            this.RaiseAndSetIfChanged(ref currentLanguage, value);
            RunOnUi(() => ChangeLanguage(value?.Name ?? "en-US"));
        }
    }

    public IReadOnlyList<string> AvailableVersions { get; } =
    [
        "VanillaClassic",
        "VanillaWarband",
        "Warband1171",
        "VanillaWFS",
        "WSE320",
        "WSE450",
        "Caribbean",
    ];

    public IReadOnlyList<CultureInfo> AvailableLanguages { get; } =
    [
        new CultureInfo("en-US"),
        new CultureInfo("ru-RU"),
    ];

    public string AppTitle => localization["AppTitle"];
    public string InputSectionTitle => localization["InputSectionTitle"];
    public string InputPlaceholder => localization["InputPlaceholder"];
    public string BrowseButton => localization["BrowseButton"];
    public string OutputSectionTitle => localization["OutputSectionTitle"];
    public string OutputPlaceholder => localization["OutputPlaceholder"];
    public string EngineVersionTitle => localization["EngineVersionTitle"];
    public string DecompileButton => localization["DecompileButton"];
    public string StatusReady => localization["StatusReady"];
    public string SelectedInput => localization["SelectedInput"];
    public string OutputFolder => localization["OutputFolder"];
    public string ErrorNoInput => localization["ErrorNoInput"];
    public string ErrorNoOutput => localization["ErrorNoOutput"];
    public string ErrorCreateOutput => localization["ErrorCreateOutput"];
    public string StatusDecompiling => localization["StatusDecompiling"];
    public string StatusCompleted => localization["StatusCompleted"];
    public string StatusError => localization["StatusError"];
    public string FailedToProcess => localization["FailedToProcess"];

    public ReactiveCommand<Unit, Unit> BrowseInputCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseOutputCommand { get; }
    public ReactiveCommand<Unit, Unit> DecompileCommand { get; }

    private void ChangeLanguage(string languageCode)
    {
        localization.SetLanguage(languageCode);
        UpdateStatusMessage();
        this.RaisePropertyChanged(string.Empty);
    }

    private void UpdateStatusMessage() => StatusMessage = StatusReady;

    private void OnLogMessageReceived(string message)
    {
        RunOnUi(() =>
        {
            LogOutput += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        });
    }

    private async Task BrowseInputAsync()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow);
        if (topLevel is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = InputSectionTitle,
                AllowMultiple = false,
            });

        if (files.Count == 0)
            return;

        RunOnUi(() =>
        {
            InputPath = files[0].Path.LocalPath;
            StatusMessage = $"{SelectedInput}: {Path.GetFileName(InputPath)}";
        });
    }

    private async Task BrowseOutputAsync()
    {
        var topLevel = TopLevel.GetTopLevel(App.MainWindow);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = OutputSectionTitle });

        if (folders.Count == 0)
            return;

        RunOnUi(() =>
        {
            OutputPath = folders[0].Path.LocalPath;
            StatusMessage = $"{OutputFolder}: {OutputPath}";
        });
    }

    private async Task DecompileAsync()
    {
        var currentInputPath = InputPath;
        var currentOutputPath = OutputPath;
        var currentVersion = SelectedVersion;

        if (string.IsNullOrWhiteSpace(currentInputPath))
        {
            AddError(ErrorNoInput, ErrorNoInput);
            return;
        }

        if (string.IsNullOrWhiteSpace(currentOutputPath))
        {
            AddError(ErrorNoOutput, ErrorNoOutput);
            return;
        }

        decompilationCancellation?.Dispose();
        decompilationCancellation = new CancellationTokenSource();
        var cancellationToken = decompilationCancellation.Token;

        RunOnUi(() =>
        {
            IsIndeterminateProgress = true;
            Progress = 0;
            StatusMessage = StatusDecompiling;
        });

        try
        {
            var progressReporter = new Progress<DecompilationProgress>(UpdateProgress);
            await Decomp.Core.Decompiler.DecompileAsync(
                currentInputPath,
                currentOutputPath,
                currentVersion,
                progressReporter,
                cancellationToken);
            RunOnUi(() => StatusMessage = StatusCompleted);
        }
        catch (OperationCanceledException)
        {
            RunOnUi(() => StatusMessage = StatusReady);
        }
        catch (Exception exception)
        {
            RunOnUi(() =>
            {
                StatusMessage = StatusError;
                LogOutput += $"[FATAL ERROR] {exception.Message}{Environment.NewLine}";
                if (exception.InnerException is not null)
                    LogOutput += $"[DETAIL] {exception.InnerException.Message}{Environment.NewLine}";
            });
        }
        finally
        {
            RunOnUi(() => IsIndeterminateProgress = false);
        }
    }

    private void UpdateProgress(DecompilationProgress update)
    {
        RunOnUi(() =>
        {
            if (update.TotalFiles > 0)
            {
                Progress = update.ProcessedFiles * 100d / update.TotalFiles;
                IsIndeterminateProgress = false;
            }

            StatusMessage = update.Message;
        });
    }

    private void AddError(string logMessage, string status)
    {
        RunOnUi(() =>
        {
            LogOutput += $"[ERROR] {logMessage}{Environment.NewLine}";
            StatusMessage = status;
        });
    }

    private void RunOnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    private static string NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Decomp.Core.Decompiler.NormalizePath(path);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        decompilationCancellation?.Cancel();
        decompilationCancellation?.Dispose();
        Decompiler.LogMessage -= OnLogMessageReceived;
        disposed = true;
        GC.SuppressFinalize(this);
    }
}
