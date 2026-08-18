using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseService :
        INotifyPropertyChanged
    {
        private static readonly Lazy<LicenseService> LazyInstance =
            new(() => new LicenseService());

        private readonly LicenseStorageService _storageService;

        private LicenseInfo _currentLicense;

        private LicenseService()
        {
            _storageService =
                new LicenseStorageService();

            _currentLicense =
                _storageService.Load();
        }

        public static LicenseService Instance =>
            LazyInstance.Value;

        public LicenseInfo CurrentLicense =>
            _currentLicense;

        public LicenseStatus Status =>
            _currentLicense.Status;

        public bool IsActive =>
            _currentLicense.IsActive;

        public int? RemainingDays =>
            _currentLicense.RemainingDays;

        public event EventHandler? LicenseChanged;

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public void SetLicense(
            LicenseInfo license)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            _storageService.Save(
                license);

            _currentLicense =
                license;

            OnLicenseChanged();
        }

        public void ClearLicense()
        {
            _storageService.Delete();

            _currentLicense =
                new LicenseInfo
                {
                    Status =
                        LicenseStatus.Unlicensed
                };

            OnLicenseChanged();
        }

        public void ReloadLicense()
        {
            _currentLicense =
                _storageService.Load();

            OnLicenseChanged();
        }

        private void OnLicenseChanged()
        {
            OnPropertyChanged(
                nameof(CurrentLicense));

            OnPropertyChanged(
                nameof(Status));

            OnPropertyChanged(
                nameof(IsActive));

            OnPropertyChanged(
                nameof(RemainingDays));

            LicenseChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

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