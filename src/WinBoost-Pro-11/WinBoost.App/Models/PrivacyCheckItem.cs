using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinBoost.App.Models
{
    public class PrivacyCheckItem : INotifyPropertyChanged
    {
        private string _status = "Neverificat";

        public string Id { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Status
        {
            get => _status;

            set
            {
                if (_status == value)
                    return;

                _status = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusLevel));
            }
        }

        public string StatusLevel =>
            Status switch
            {
                "Dezactivat" => "Good",
                "Date obligatorii" => "Good",

                "Activat" => "Attention",
                "Date opționale" => "Attention",
                "Date îmbunătățite" => "Attention",

                _ => "Neutral"
            };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}