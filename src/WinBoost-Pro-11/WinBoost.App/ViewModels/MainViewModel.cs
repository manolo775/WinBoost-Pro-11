using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinBoost.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _currentPage = "Dashboard";

        public string CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}