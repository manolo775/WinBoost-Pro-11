using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        public int TotalServices
        {
            get => _totalServices;

            set
            {
                if (_totalServices == value)
                {
                    return;
                }

                _totalServices = value;
                OnPropertyChanged();
            }
        }

        public int RunningServices
        {
            get => _runningServices;

            set
            {
                if (_runningServices == value)
                {
                    return;
                }

                _runningServices = value;
                OnPropertyChanged();
            }
        }

        public int CriticalServices
        {
            get => _criticalServices;

            set
            {
                if (_criticalServices == value)
                {
                    return;
                }

                _criticalServices = value;
                OnPropertyChanged();
            }
        }

        public int RecommendedServices
        {
            get => _recommendedServices;

            set
            {
                if (_recommendedServices == value)
                {
                    return;
                }

                _recommendedServices = value;
                OnPropertyChanged();
            }
        }

        public int SafeToOptimizeServices
        {
            get => _safeToOptimizeServices;

            set
            {
                if (_safeToOptimizeServices == value)
                {
                    return;
                }

                _safeToOptimizeServices = value;
                OnPropertyChanged();
            }
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
                OnPropertyChanged(nameof(EstimatedHealthGainText));
            }
        }

        public string EstimatedHealthGainText =>
            EstimatedHealthGain > 0
                ? $"+{EstimatedHealthGain} Health"
                : "Fără câștig disponibil";

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