using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace DecompilerGUI
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                HandlePlatformSpecificError(ex);
                Environment.Exit(1);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();

            // Platform-specific configurations
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                builder = builder
                    .With(new MacOSPlatformOptions
                    {
                        ShowInDock = true,
                        DisableDefaultApplicationMenuItems = false
                    });
            }

            return builder;
        }

        private static void HandlePlatformSpecificError(Exception ex)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine($"Fatal error on Windows: {ex.Message}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine($"Fatal error on macOS: {ex.Message}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine($"Fatal error on Linux: {ex.Message}");
            }
            else
            {
                Console.WriteLine($"Fatal error on unknown platform: {ex.Message}");
            }

            Console.WriteLine(ex.StackTrace);
        }
    }
}
