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
                // Store the previous language to check if it has changed
                if (_currentLanguage != value)
                {
                    this.RaiseAndSetIfChanged(ref _currentLanguage, value);
                    ChangeLanguage(value.Name);
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

        // Propriedades para localização
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
            _currentLanguage = AvailableLanguages[0]; // Define o idioma padrão
            UpdateStatusMessage();

            BrowseInputCommand = ReactiveCommand.CreateFromTask(BrowseInputAsync);
            BrowseOutputCommand = ReactiveCommand.CreateFromTask(BrowseOutputAsync);
            DecompileCommand = ReactiveCommand.CreateFromTask(DecompileAsync);

            Decompiler.LogMessage += OnLogMessageReceived;
        }

        private void ChangeLanguage(string languageCode)
        {
            _localization.SetLanguage(languageCode);
            UpdateStatusMessage();
            this.RaisePropertyChanged(string.Empty); // Notifica todas as propriedades
        }

        private void UpdateStatusMessage()
        {
            StatusMessage = StatusReady;
        }

        private void OnLogMessageReceived(string message)
        {
            LogOutput += $"{DateTime.Now:HH:mm:ss} {message}\n";
        }

        private async Task BrowseInputAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            var options = new FilePickerOpenOptions
            {
                Title = InputSectionTitle,
                AllowMultiple = false
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                InputPath = files[0].Path.LocalPath;
                StatusMessage = $"{SelectedInput}: {Path.GetFileName(InputPath)}";
            }
        }

        private async Task BrowseOutputAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            var options = new FolderPickerOpenOptions
            {
                Title = OutputSectionTitle
            };

            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (folder.Count > 0)
            {
                OutputPath = folder[0].Path.LocalPath;
                StatusMessage = $"{OutputFolder}: {OutputPath}";
            }
        }

        private async Task DecompileAsync()
        {
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                LogOutput += $"[ERROR] {ErrorNoInput}\n";
                StatusMessage = ErrorNoInput;
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                LogOutput += $"[ERROR] {ErrorNoOutput}\n";
                StatusMessage = ErrorNoOutput;
                return;
            }

            if (!Directory.Exists(OutputPath))
            {
                try
                {
                    Directory.CreateDirectory(OutputPath);
                }
                catch (Exception ex)
                {
                    LogOutput += $"[ERROR] {ErrorCreateOutput}: {ex.Message}\n";
                    StatusMessage = ErrorCreateOutput;
                    return;
                }
            }

            IsIndeterminateProgress = true;
            Progress = 0;
            StatusMessage = StatusDecompiling;

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
                                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                                Decompiler.Decompile(file, outputFile, SelectedVersion);
                                processedFiles++;

                                Progress = (double)processedFiles / totalFiles * 100;
                            }
                            catch (Exception ex)
                            {
                                LogOutput += $"[ERROR] {FailedToProcess} {Path.GetFileName(file)}: {ex.Message}\n";
                            }
                        }
                    }
                    else
                    {
                        var outputFile = Path.Combine(OutputPath, Path.GetFileName(InputPath));
                        Decompiler.Decompile(InputPath, outputFile, SelectedVersion);
                    }
                });

                StatusMessage = StatusCompleted;
            }
            catch (Exception ex)
            {
                StatusMessage = StatusError;
                LogOutput += $"[FATAL ERROR] {ex.Message}\n";
                if (ex.InnerException != null)
                {
                    LogOutput += $"[DETAIL] {ex.InnerException.Message}\n";
                }
            }
            finally
            {
                IsIndeterminateProgress = false;
            }
        }

        private bool IsSupportedFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return path.Replace('/', '\\');
            }
            else
            {
                return path.Replace('\\', '/');
            }
        }
    }
}
