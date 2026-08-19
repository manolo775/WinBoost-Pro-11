using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class PendingPurchaseService :
        INotifyPropertyChanged
    {
        private readonly PendingPurchaseStorageService
            _storageService;

        private PendingPurchaseInfo?
            _currentPurchase;

        public static PendingPurchaseService Instance
        {
            get;
        } = new PendingPurchaseService();

        private PendingPurchaseService()
        {
            _storageService =
                new PendingPurchaseStorageService();

            _currentPurchase =
                _storageService.Load();
        }

        public PendingPurchaseInfo?
            CurrentPurchase =>
                _currentPurchase;

        public bool HasPendingPurchase =>
            _currentPurchase != null &&
            !string.IsNullOrWhiteSpace(
                _currentPurchase.CustomerEmail) &&
            !string.IsNullOrWhiteSpace(
                _currentPurchase.PurchaseSessionId) &&
            _currentPurchase.Plan !=
                LicensePlan.Unknown;

        public event EventHandler?
            PendingPurchaseChanged;

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public void SetPendingPurchase(
            PendingPurchaseInfo purchase)
        {
            ArgumentNullException.ThrowIfNull(
                purchase);

            _storageService.Save(
                purchase);

            _currentPurchase =
                purchase;

            NotifyChanged();
        }

        public void ClearPendingPurchase()
        {
            _storageService.Delete();

            _currentPurchase =
                null;

            NotifyChanged();
        }

        public void ReloadPendingPurchase()
        {
            _currentPurchase =
                _storageService.Load();

            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnPropertyChanged(
                nameof(CurrentPurchase));

            OnPropertyChanged(
                nameof(HasPendingPurchase));

            PendingPurchaseChanged?.Invoke(
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