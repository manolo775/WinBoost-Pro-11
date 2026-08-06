using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class PrivacyCheckItem :
        INotifyPropertyChanged
    {
        private string _statusResourceKey =
            "PrivacyStatusNotScanned";

        public string Id
        {
            get;
            init;
        } =
            string.Empty;

        public string TitleResourceKey
        {
            get;
            init;
        } =
            string.Empty;

        public string DescriptionResourceKey
        {
            get;
            init;
        } =
            string.Empty;

        public string StatusResourceKey
        {
            get => _statusResourceKey;

            set
            {
                if (_statusResourceKey == value)
                {
                    return;
                }

                _statusResourceKey =
                    value ?? string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(Status));

                OnPropertyChanged(
                    nameof(StatusLevel));
            }
        }

        public string Title =>
            LocalizationHelper.Get(
                TitleResourceKey);

        public string Description =>
            LocalizationHelper.Get(
                DescriptionResourceKey);

        public string Status =>
            LocalizationHelper.Get(
                StatusResourceKey);

        public string StatusLevel =>
            StatusResourceKey switch
            {
                "PrivacyStatusDisabled" =>
                    "Good",

                "PrivacyStatusDiagnosticMinimal" =>
                    "Good",

                "PrivacyStatusDiagnosticRequired" =>
                    "Good",

                "PrivacyStatusEnabled" =>
                    "Attention",

                "PrivacyStatusDiagnosticEnhanced" =>
                    "Attention",

                "PrivacyStatusDiagnosticOptional" =>
                    "Attention",

                _ =>
                    "Neutral"
            };

        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(
                nameof(Title));

            OnPropertyChanged(
                nameof(Description));

            OnPropertyChanged(
                nameof(Status));
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}