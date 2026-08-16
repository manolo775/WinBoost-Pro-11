using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Services.Navigation;

namespace WinBoost.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _currentPage = "Dashboard";

        public MainViewModel()
        {
            AppNavigationService.NavigationRequested +=
                AppNavigationService_NavigationRequested;
        }

        public string CurrentPage
        {
            get => _currentPage;

            set
            {
                if (_currentPage == value)
                {
                    return;
                }

                _currentPage = value;

                OnPropertyChanged();
            }
        }

        private void AppNavigationService_NavigationRequested(
            string page)
        {
            CurrentPage = page;
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}