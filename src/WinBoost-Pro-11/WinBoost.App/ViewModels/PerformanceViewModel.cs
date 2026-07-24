using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : INotifyPropertyChanged
    {
        private string _status = "Ready";

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value)
                    return;

                _status = value;
                OnPropertyChanged();
            }
        }

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