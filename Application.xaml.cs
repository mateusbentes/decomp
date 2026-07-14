using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Decomp
{
    public partial class Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            CommandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

            // Remover a dependência do registro do Windows
            // var key = Registry.CurrentUser.OpenSubKey("Software\\WMD");
            // if (key == null) return;
            // var language = key.GetValue("Language") as string;
            // if (language == "Russian" || language == "English") Language = language;
        }

        public static IList<string> CommandLineArgs { get; private set; }

        public static string StartupPath => Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);

        private static string _language = "";
        public static string Language
        {
            get => _language;
            set
            {
                _language = value;
                var newDict = new ResourceDictionary { Source = new Uri($"Languages/{value}.xaml", UriKind.Relative) };

                var oldDict = (from d in Current.Resources.MergedDictionaries where d.Source != null && d.Source.OriginalString.StartsWith("Languages/", StringComparison.OrdinalIgnoreCase) select d).First();

                if (oldDict != null)
                {
                    var i = Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Current.Resources.MergedDictionaries.Remove(oldDict);
                    Current.Resources.MergedDictionaries.Insert(i, newDict);
                }
                else
                {
                    Current.Resources.MergedDictionaries.Add(newDict);
                }
            }
        }

        public static string GetResource(string s) => (string)Current.FindResource(s);

        public static string Greeting => "Hey there!";
    }
}
