using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;

namespace DecompilerGUI.Services
{
    public class LocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public LocalizationService()
        {
            _resourceManager = new ResourceManager("decomp.Languages.Strings", typeof(LocalizationService).Assembly);

            // Set default culture based on OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _currentCulture = CultureInfo.CurrentCulture;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _currentCulture = GetMacOSCulture();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _currentCulture = GetLinuxCulture();
            }
            else
            {
                _currentCulture = CultureInfo.CurrentCulture;
            }

            Thread.CurrentThread.CurrentCulture = _currentCulture;
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
        }

        public string this[string key] => _resourceManager.GetString(key, _currentCulture) ?? key;

        public void SetLanguage(string languageCode)
        {
            _currentCulture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = _currentCulture;
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
        }

        public CultureInfo CurrentCulture => _currentCulture;

        private CultureInfo GetMacOSCulture()
        {
            try
            {
                // Try to get macOS system language
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "defaults",
                        Arguments = "read -g AppleLocale",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    return new CultureInfo(output.Trim());
                }
            }
            catch
            {
                // Fallback to current culture if we can't determine macOS language
            }

            return CultureInfo.CurrentCulture;
        }

        private CultureInfo GetLinuxCulture()
        {
            try
            {
                // Try to get Linux system language from environment variables
                string lang = Environment.GetEnvironmentVariable("LANG") ??
                             Environment.GetEnvironmentVariable("LC_ALL") ??
                             Environment.GetEnvironmentVariable("LC_MESSAGES") ??
                             "en-US";

                // Extract language code from format like "en_US.UTF-8"
                string langCode = lang.Split('.')[0].Replace('_', '-');
                return new CultureInfo(langCode);
            }
            catch
            {
                // Fallback to current culture if we can't determine Linux language
                return CultureInfo.CurrentCulture;
            }
        }
    }
}
