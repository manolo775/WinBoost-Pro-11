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

        private readonly LicenseStorageService
            _storageService;

        private readonly SignedLicenseStorageService
            _signedLicenseStorageService;

        private readonly LicenseResponseValidator
            _licenseResponseValidator;

        private LicenseInfo
            _currentLicense;

        private LicenseService()
        {
            _storageService =
                new LicenseStorageService();

            _signedLicenseStorageService =
                new SignedLicenseStorageService();

            _licenseResponseValidator =
                new LicenseResponseValidator();

            _currentLicense =
                LoadVerifiedLicense();
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

        public event EventHandler?
            LicenseChanged;

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public void SetLicense(
            LicenseInfo license)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            // license.dat remains only a local cache.
            // It is no longer trusted as the source of truth
            // when WinBoost starts.
            _storageService.Save(
                license);

            _currentLicense =
                license;

            OnLicenseChanged();
        }

        public void ClearLicense()
        {
            _storageService.Delete();

            _signedLicenseStorageService
                .Delete();

            _currentLicense =
                CreateUnlicensedLicense();

            OnLicenseChanged();
        }

        public void ReloadLicense()
        {
            _currentLicense =
                LoadVerifiedLicense();

            OnLicenseChanged();
        }

        private LicenseInfo LoadVerifiedLicense()
        {
            SignedLicenseResponse? signedLicense =
                _signedLicenseStorageService
                    .Load();

            if (signedLicense == null)
            {
                return CreateUnlicensedLicense();
            }

            if (!LicenseSecurityConfiguration
                    .HasPublicKey)
            {
                return CreateInvalidLicense(
                    signedLicense);
            }

            LicenseActivationResult validationResult =
                _licenseResponseValidator
                    .Validate(
                        signedLicense,
                        LicenseSecurityConfiguration
                            .PublicKeyPem);

            if (validationResult.IsSuccessful &&
                validationResult.License != null)
            {
                LicenseInfo verifiedLicense =
                    validationResult.License;

                TryUpdateLocalCache(
                    verifiedLicense);

                return verifiedLicense;
            }

            if (validationResult.Status ==
                LicenseActivationStatus.Expired)
            {
                return CreateExpiredLicense(
                    signedLicense);
            }

            return CreateInvalidLicense(
                signedLicense);
        }

        private void TryUpdateLocalCache(
            LicenseInfo license)
        {
            try
            {
                _storageService.Save(
                    license);
            }
            catch
            {
                // The signed license remains the source of truth.
                // Failure to refresh the cache must not invalidate
                // an otherwise valid signed license.
            }
        }

        private static LicenseInfo
            CreateUnlicensedLicense()
        {
            return new LicenseInfo
            {
                Status =
                    LicenseStatus.Unlicensed
            };
        }

        private static LicenseInfo
            CreateExpiredLicense(
                SignedLicenseResponse signedLicense)
        {
            return new LicenseInfo
            {
                Status =
                    LicenseStatus.Expired,

                LicenseId =
                    signedLicense.LicenseId,

                CustomerEmail =
                    signedLicense.CustomerEmail,

                LicenseType =
                    signedLicense.LicenseType,

                Plan =
                    signedLicense.Plan,

                ActivatedAt =
                    signedLicense.ActivatedAt,

                ExpiresAt =
                    signedLicense.ExpiresAt,

                LicensedTo =
                    signedLicense.CustomerEmail,

                LicenseKey =
                    string.Empty
            };
        }

        private static LicenseInfo
            CreateInvalidLicense(
                SignedLicenseResponse signedLicense)
        {
            return new LicenseInfo
            {
                Status =
                    LicenseStatus.Invalid,

                LicenseId =
                    signedLicense.LicenseId,

                CustomerEmail =
                    signedLicense.CustomerEmail,

                LicenseType =
                    signedLicense.LicenseType,

                Plan =
                    signedLicense.Plan,

                ActivatedAt =
                    signedLicense.ActivatedAt,

                ExpiresAt =
                    signedLicense.ExpiresAt,

                LicensedTo =
                    signedLicense.CustomerEmail,

                LicenseKey =
                    string.Empty
            };
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