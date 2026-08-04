using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace WinBoost.App.Localization
{
    public sealed class LanguageManager
    {
        private const string RomanianDictionary =
            "/WinBoost.App;component/Localization/Strings.ro.xaml";

        private const string EnglishDictionary =
            "/WinBoost.App;component/Localization/Strings.en.xaml";

        private static readonly Lazy<LanguageManager>
            LazyInstance =
                new(() => new LanguageManager());

        private LanguageManager()
        {
        }

        public static LanguageManager Instance =>
            LazyInstance.Value;

        public Language CurrentLanguage
        {
            get;
            private set;
        } = Language.Romanian;

        public event EventHandler?
            LanguageChanged;

        public void SetLanguage(
            Language language)
        {
            CultureInfo culture =
    language == Language.English
        ? CultureInfo.GetCultureInfo("en-US")
        : CultureInfo.GetCultureInfo("ro-RO");

            CultureInfo.CurrentCulture =
                culture;

            CultureInfo.CurrentUICulture =
                culture;
            string dictionaryPath =
                language == Language.English
                    ? EnglishDictionary
                    : RomanianDictionary;

            ResourceDictionary newDictionary =
                new()
                {
                    Source =
                        new Uri(
                            dictionaryPath,
                            UriKind.RelativeOrAbsolute)
                };

            var dictionaries =
                Application.Current.Resources
                    .MergedDictionaries;

            ResourceDictionary? oldDictionary =
                dictionaries.FirstOrDefault(
                    dictionary =>
                        IsLanguageDictionary(
                            dictionary.Source));

            if (oldDictionary != null)
            {
                int index =
                    dictionaries.IndexOf(
                        oldDictionary);

                dictionaries[index] =
                    newDictionary;
            }
            else
            {
                dictionaries.Add(
                    newDictionary);
            }

            CurrentLanguage =
                language;

            LanguageChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        private static bool IsLanguageDictionary(
            Uri? source)
        {
            if (source == null)
            {
                return false;
            }

            string path =
                source.OriginalString;

            return path.Contains(
                       "Strings.ro.xaml",
                       StringComparison.OrdinalIgnoreCase) ||
                   path.Contains(
                       "Strings.en.xaml",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}