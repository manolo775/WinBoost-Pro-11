using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public class ServicesHealthSummary : INotifyPropertyChanged
    {
        private int _totalServices;
        private int _runningServices;
        private int _criticalServices;
        private int _recommendedServices;
        private int _safeToOptimizeServices;
        private int _estimatedHealthGain;

        public ServicesHealthSummary()
        {
            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public int TotalServices
        {
            get => _totalServices;
            set => SetField(ref _totalServices, value);
        }

        public int RunningServices
        {
            get => _runningServices;
            set => SetField(ref _runningServices, value);
        }

        public int CriticalServices
        {
            get => _criticalServices;
            set => SetField(ref _criticalServices, value);
        }

        public int RecommendedServices
        {
            get => _recommendedServices;
            set => SetField(ref _recommendedServices, value);
        }

        public int SafeToOptimizeServices
        {
            get => _safeToOptimizeServices;
            set => SetField(ref _safeToOptimizeServices, value);
        }

        public int EstimatedHealthGain
        {
            get => _estimatedHealthGain;

            set
            {
                int normalizedValue =
                    value < 0
                        ? 0
                        : value;

                if (_estimatedHealthGain == normalizedValue)
                {
                    return;
                }

                _estimatedHealthGain = normalizedValue;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(EstimatedHealthGainText));
            }
        }

        public string EstimatedHealthGainText =>
            EstimatedHealthGain > 0
                ? LocalizationHelper.Format(
                    "ServicesEstimatedGainValue",
                    EstimatedHealthGain)
                : LocalizationHelper.Get(
                    "ServicesNoEstimatedGain");

        public void RefreshLocalizedText()
        {
            OnPropertyChanged(
                nameof(EstimatedHealthGainText));
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            System.EventArgs e)
        {
            RefreshLocalizedText();
        }

        private void SetField(
            ref int field,
            int value,
            [CallerMemberName]
            string? propertyName = null)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
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