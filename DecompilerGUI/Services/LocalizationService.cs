using System.Globalization;
using System.Resources;
using System.Threading;

namespace DecompilerGUI.Services
{
    public class LocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public LocalizationService()
        {
            _resourceManager = new ResourceManager("DecompilerGUI.Languages.Strings", typeof(LocalizationService).Assembly);
            _currentCulture = CultureInfo.CurrentCulture;
        }

        public string this[string key] => _resourceManager.GetString(key, _currentCulture) ?? key;

        public void SetLanguage(string languageCode)
        {
            _currentCulture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = _currentCulture;
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
        }

        public CultureInfo CurrentCulture => _currentCulture;
    }
}
