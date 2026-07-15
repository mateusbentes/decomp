using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Decomp.Core;
using DecompilerGUI.Services;
using ReactiveUI;

namespace DecompilerGUI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly LocalizationService _localization;
        private string _inputPath = string.Empty;
        private string _outputPath = string.Empty;
        private string _selectedVersion = "VanillaWarband";
        private double _progress;
        private bool _isIndeterminateProgress;
        private string _logOutput = string.Empty;
        private string _statusMessage = string.Empty;
        private CultureInfo _currentLanguage;

        public string InputPath
        {
            get => _inputPath;
            set => this.RaiseAndSetIfChanged(ref _inputPath, NormalizePath(value));
        }

        public string OutputPath
        {
            get => _outputPath;
            set => this.RaiseAndSetIfChanged(ref _outputPath, NormalizePath(value));
        }

        public string SelectedVersion
        {
            get => _selectedVersion;
            set => this.RaiseAndSetIfChanged(ref _selectedVersion, value);
        }

        public double Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public bool IsIndeterminateProgress
        {
            get => _isIndeterminateProgress;
            set => this.RaiseAndSetIfChanged(ref _isIndeterminateProgress, value);
        }

        public string LogOutput
        {
            get => _logOutput;
            set => this.RaiseAndSetIfChanged(ref _logOutput, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public CultureInfo CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    this.RaiseAndSetIfChanged(ref _currentLanguage, value);
                    Dispatcher.UIThread.Post(() => ChangeLanguage(value?.Name ?? "en-US"));
                }
            }
        }

        public List<string> AvailableVersions { get; } = new()
        {
            "VanillaClassic", "VanillaWarband", "Warband1171", "VanillaWFS", "WSE320", "WSE450", "Caribbean"
        };

        public List<CultureInfo> AvailableLanguages { get; } = new()
        {
            new CultureInfo("en-US"),
            new CultureInfo("ru-RU")
        };

        public string AppTitle => _localization["AppTitle"];
        public string InputSectionTitle => _localization["InputSectionTitle"];
        public string InputPlaceholder => _localization["InputPlaceholder"];
        public string BrowseButton => _localization["BrowseButton"];
        public string OutputSectionTitle => _localization["OutputSectionTitle"];
        public string OutputPlaceholder => _localization["OutputPlaceholder"];
        public string EngineVersionTitle => _localization["EngineVersionTitle"];
        public string DecompileButton => _localization["DecompileButton"];
        public string StatusReady => _localization["StatusReady"];
        public string SelectedInput => _localization["SelectedInput"];
        public string OutputFolder => _localization["OutputFolder"];
        public string ErrorNoInput => _localization["ErrorNoInput"];
        public string ErrorNoOutput => _localization["ErrorNoOutput"];
        public string ErrorCreateOutput => _localization["ErrorCreateOutput"];
        public string StatusDecompiling => _localization["StatusDecompiling"];
        public string StatusCompleted => _localization["StatusCompleted"];
        public string StatusError => _localization["StatusError"];
        public string FailedToProcess => _localization["FailedToProcess"];

        public ReactiveCommand<Unit, Unit> BrowseInputCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseOutputCommand { get; }
        public ReactiveCommand<Unit, Unit> DecompileCommand { get; }

        public MainWindowViewModel()
        {
            _localization = new LocalizationService();
            _currentLanguage = AvailableLanguages[0];

            BrowseInputCommand = ReactiveCommand.CreateFromTask(BrowseInputAsync, outputScheduler: RxApp.MainThreadScheduler);
            BrowseOutputCommand = ReactiveCommand.CreateFromTask(BrowseOutputAsync, outputScheduler: RxApp.MainThreadScheduler);
            DecompileCommand = ReactiveCommand.CreateFromTask(DecompileAsync, outputScheduler: RxApp.MainThreadScheduler);

            Decompiler.LogMessage += OnLogMessageReceived;
            UpdateStatusMessage();
        }

        private void ChangeLanguage(string languageCode)
        {
            _localization.SetLanguage(languageCode);
            UpdateStatusMessage();
            this.RaisePropertyChanged(string.Empty);
        }

        private void UpdateStatusMessage()
        {
            Dispatcher.UIThread.Post(() => StatusMessage = StatusReady);
        }

        private void OnLogMessageReceived(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogOutput += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
                this.RaisePropertyChanged(nameof(LogOutput));
            });
        }

        private async Task BrowseInputAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            if (topLevel == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = InputSectionTitle,
                AllowMultiple = false
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    InputPath = files[0].Path.LocalPath;
                    StatusMessage = $"{SelectedInput}: {Path.GetFileName(InputPath)}";
                });
            }
        }

        private async Task BrowseOutputAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            if (topLevel == null) return;

            var options = new FolderPickerOpenOptions
            {
                Title = OutputSectionTitle
            };

            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (folder.Count > 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    OutputPath = folder[0].Path.LocalPath;
                    StatusMessage = $"{OutputFolder}: {OutputPath}";
                });
            }
        }

        private async Task DecompileAsync()
        {
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LogOutput += $"[ERROR] {ErrorNoInput}{Environment.NewLine}";
                    StatusMessage = ErrorNoInput;
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LogOutput += $"[ERROR] {ErrorNoOutput}{Environment.NewLine}";
                    StatusMessage = ErrorNoOutput;
                });
                return;
            }

            try
            {
                Directory.CreateDirectory(OutputPath);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LogOutput += $"[ERROR] {ErrorCreateOutput}: {ex.Message}{Environment.NewLine}";
                    StatusMessage = ErrorCreateOutput;
                });
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                IsIndeterminateProgress = true;
                Progress = 0;
                StatusMessage = StatusDecompiling;
            });

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(InputPath))
                    {
                        var files = Directory.GetFiles(InputPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => IsSupportedFileType(f))
                            .ToList();

                        int totalFiles = files.Count;
                        int processedFiles = 0;

                        foreach (var file in files)
                        {
                            try
                            {
                                var relativePath = GetRelativePath(InputPath, file);
                                var outputFile = Path.Combine(OutputPath, relativePath);
                                var outputDir = Path.GetDirectoryName(outputFile);
                                if (outputDir != null)
                                {
                                    Directory.CreateDirectory(outputDir);
                                }

                                Decompiler.Decompile(file, outputFile, SelectedVersion);
                                processedFiles++;

                                Dispatcher.UIThread.Post(() => Progress = (double)processedFiles / totalFiles * 100);
                            }
                            catch (Exception ex)
                            {
                                Decompiler.RaiseLogMessage($"[ERROR] {FailedToProcess} {Path.GetFileName(file)}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        var outputFile = Path.Combine(OutputPath, Path.GetFileName(InputPath));
                        Decompiler.Decompile(InputPath, outputFile, SelectedVersion);
                    }
                });

                Dispatcher.UIThread.Post(() => StatusMessage = StatusCompleted);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = StatusError;
                    LogOutput += $"[FATAL ERROR] {ex.Message}{Environment.NewLine}";
                    if (ex.InnerException != null)
                    {
                        LogOutput += $"[DETAIL] {ex.InnerException.Message}{Environment.NewLine}";
                    }
                });
            }
            finally
            {
                Dispatcher.UIThread.Post(() => IsIndeterminateProgress = false);
            }
        }

        private bool IsSupportedFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;
            return extension is ".txt" or ".vsh" or ".psh" or ".fxc" or ".glsl";
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            var fromUri = new Uri(NormalizePath(fromPath) + Path.DirectorySeparatorChar);
            var toUri = new Uri(NormalizePath(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? path.Replace('/', '\\')
                : path.Replace('\\', '/');
        }
    }
}
