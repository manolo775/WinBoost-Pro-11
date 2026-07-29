using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class AppsViewModel : INotifyPropertyChanged
    {
        private readonly InstalledAppsScanner _installedAppsScanner;

        private bool _isScanning;
        private string _searchText = string.Empty;

        private string _scanStatus =
            "Apasă Scan Apps pentru a căuta aplicațiile instalate.";

        private string _scanBadgeText = "Neverificat";

        private Brush _scanBadgeBrush = Brushes.LightGray;

        public ObservableCollection<InstalledAppInfo> Applications { get; }

        public ICollectionView FilteredApplications { get; }

        public ICommand ScanAppsCommand { get; }

        public string SearchText
        {
            get => _searchText;

            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;

                OnPropertyChanged();
                FilteredApplications.Refresh();
            }
        }

        public string ScanStatus
        {
            get => _scanStatus;

            private set
            {
                if (_scanStatus == value)
                    return;

                _scanStatus = value;
                OnPropertyChanged();
            }
        }

        public string ScanBadgeText
        {
            get => _scanBadgeText;

            private set
            {
                if (_scanBadgeText == value)
                    return;

                _scanBadgeText = value;
                OnPropertyChanged();
            }
        }

        public Brush ScanBadgeBrush
        {
            get => _scanBadgeBrush;

            private set
            {
                if (_scanBadgeBrush == value)
                    return;

                _scanBadgeBrush = value;
                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;

            private set
            {
                if (_isScanning == value)
                    return;

                _isScanning = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ScanButtonText));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? "Scanning..."
                : "Scan Apps";

        public AppsViewModel()
        {
            _installedAppsScanner =
                new InstalledAppsScanner();

            Applications =
                new ObservableCollection<InstalledAppInfo>();

            FilteredApplications =
                CollectionViewSource.GetDefaultView(Applications);

            FilteredApplications.Filter =
                FilterApplication;

            ScanAppsCommand =
                new RelayCommand(
                    async _ => await ScanAppsAsync(),
                    _ => !IsScanning);
        }

        private bool FilterApplication(object item)
        {
            if (item is not InstalledAppInfo application)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            string search =
                SearchText.Trim();

            return ContainsText(
                       application.DisplayName,
                       search) ||
                   ContainsText(
                       application.Publisher,
                       search) ||
                   ContainsText(
                       application.Version,
                       search) ||
                   ContainsText(
                       application.InstallDate,
                       search);
        }

        private static bool ContainsText(
            object? value,
            string search)
        {
            string text =
                Convert.ToString(value) ?? string.Empty;

            return text.Contains(
                search,
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task ScanAppsAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            ScanStatus =
                "Se caută aplicațiile instalate...";

            ScanBadgeText = "Se verifică";
            ScanBadgeBrush = Brushes.Orange;

            try
            {
                var applications =
                    await _installedAppsScanner.ScanAsync();

                Applications.Clear();

                foreach (InstalledAppInfo application
                         in applications)
                {
                    Applications.Add(application);
                }

                FilteredApplications.Refresh();

                ScanStatus =
                    $"Scanare finalizată: {Applications.Count} aplicații găsite.";

                ScanBadgeText = "Verificat";
                ScanBadgeBrush = Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                ScanStatus =
                    $"Scanarea nu a putut fi finalizată: {ex.Message}";

                ScanBadgeText = "Eroare";
                ScanBadgeBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsScanning = false;
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