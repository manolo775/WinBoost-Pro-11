using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinBoost.App.Models
{
    public class StartupAppInfo : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Name { get; set; } =
            string.Empty;

        public string Command { get; set; } =
            string.Empty;

        public string Source { get; set; } =
            string.Empty;

        public RegistryHive RegistryHive { get; set; }

        public string RegistryPath { get; set; } =
            string.Empty;

        public string RegistryValueName { get; set; } =
            string.Empty;

        public bool IsEnabled
        {
            get => _isEnabled;

            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ActionText));
            }
        }

        public string Status =>
            IsEnabled
                ? "Activat"
                : "Dezactivat";

        public string ActionText =>
            IsEnabled
                ? "Dezactivează"
                : "Activează";

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}