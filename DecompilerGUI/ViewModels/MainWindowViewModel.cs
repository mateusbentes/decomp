using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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

        public string InputPath
        {
            get => _inputPath;
            set => this.RaiseAndSetIfChanged(ref _inputPath, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => this.RaiseAndSetIfChanged(ref _outputPath, value);
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

        public List<string> AvailableVersions { get; } = new()
        {
            "VanillaClassic", "VanillaWarband", "Warband1171", "VanillaWFS", "WSE320", "WSE450", "Caribbean"
        };

        public List<CultureInfo> AvailableLanguages { get; } = new()
        {
            new CultureInfo("en-US"),
            new CultureInfo("ru-RU")
        };

        public ReactiveCommand<Unit, Unit> BrowseInputCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseOutputCommand { get; }
        public ReactiveCommand<Unit, Unit> DecompileCommand { get; }
        public ReactiveCommand<string, Unit> ChangeLanguageCommand { get; }

        public MainWindowViewModel()
        {
            _localization = new LocalizationService();
            UpdateStatusMessage();

            BrowseInputCommand = ReactiveCommand.CreateFromTask(BrowseInputAsync);
            BrowseOutputCommand = ReactiveCommand.CreateFromTask(BrowseOutputAsync);
            DecompileCommand = ReactiveCommand.CreateFromTask(DecompileAsync);
            ChangeLanguageCommand = ReactiveCommand.Create<string>(ChangeLanguage);

            Decompiler.LogMessage += OnLogMessageReceived;
        }

        private void ChangeLanguage(string languageCode)
        {
            _localization.SetLanguage(languageCode);
            UpdateStatusMessage();
            this.RaisePropertyChanged(string.Empty);
        }

        private void UpdateStatusMessage()
        {
            StatusMessage = _localization["StatusReady"];
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
                Title = _localization["InputSectionTitle"],
                AllowMultiple = false
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                InputPath = files[0].Path.LocalPath;
                StatusMessage = $"{_localization["SelectedInput"]}: {Path.GetFileName(InputPath)}";
            }
        }

        private async Task BrowseOutputAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            var options = new FolderPickerOpenOptions
            {
                Title = _localization["OutputSectionTitle"]
            };

            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (folder.Count > 0)
            {
                OutputPath = folder[0].Path.LocalPath;
                StatusMessage = $"{_localization["OutputFolder"]}: {OutputPath}";
            }
        }

        private async Task DecompileAsync()
        {
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                LogOutput += $"[ERROR] {_localization["ErrorNoInput"]}\n";
                StatusMessage = _localization["ErrorNoInput"];
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                LogOutput += $"[ERROR] {_localization["ErrorNoOutput"]}\n";
                StatusMessage = _localization["ErrorNoOutput"];
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
                    LogOutput += $"[ERROR] {_localization["ErrorCreateOutput"]}: {ex.Message}\n";
                    StatusMessage = _localization["ErrorCreateOutput"];
                    return;
                }
            }

            IsIndeterminateProgress = true;
            Progress = 0;
            StatusMessage = _localization["StatusDecompiling"];

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(InputPath))
                    {
                        var files = Directory.GetFiles(InputPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".txt") || f.EndsWith(".vsh") || f.EndsWith(".psh") ||
                                       f.EndsWith(".fxc") || f.EndsWith(".glsl")).ToList();

                        int totalFiles = files.Count;
                        int processedFiles = 0;

                        foreach (var file in files)
                        {
                            try
                            {
                                var relativePath = Path.GetRelativePath(InputPath, file);
                                var outputFile = Path.Combine(OutputPath, relativePath);
                                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                                Decompiler.Decompile(file, outputFile, SelectedVersion);
                                processedFiles++;

                                Progress = (double)processedFiles / totalFiles * 100;
                            }
                            catch (Exception ex)
                            {
                                LogOutput += $"[ERROR] {_localization["FailedToProcess"]} {Path.GetFileName(file)}: {ex.Message}\n";
                            }
                        }
                    }
                    else
                    {
                        var outputFile = Path.Combine(OutputPath, Path.GetFileName(InputPath));
                        Decompiler.Decompile(InputPath, outputFile, SelectedVersion);
                    }
                });

                StatusMessage = _localization["StatusCompleted"];
            }
            catch (Exception ex)
            {
                StatusMessage = _localization["StatusError"];
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

        public string GetLocalizedString(string key)
        {
            return _localization[key];
        }
    }
}
