using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public class SystemHealthSummary : INotifyPropertyChanged
    {
        private int _performanceScore;
        private int _servicesScore;
        private int _startupScore;
        private int _privacyScore;
        private int _windowsUpdateScore;

        public SystemHealthSummary()
        {
            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public int PerformanceScore
        {
            get => _performanceScore;

            set
            {
                int normalizedValue =
                    NormalizeScore(value);

                if (_performanceScore == normalizedValue)
                {
                    return;
                }

                _performanceScore = normalizedValue;

                OnPropertyChanged();
                NotifyOverallHealthChanged();
            }
        }

        public int ServicesScore
        {
            get => _servicesScore;

            set
            {
                int normalizedValue =
                    NormalizeScore(value);

                if (_servicesScore == normalizedValue)
                {
                    return;
                }

                _servicesScore = normalizedValue;

                OnPropertyChanged();
                NotifyOverallHealthChanged();
            }
        }

        public int StartupScore
        {
            get => _startupScore;

            set
            {
                int normalizedValue =
                    NormalizeScore(value);

                if (_startupScore == normalizedValue)
                {
                    return;
                }

                _startupScore = normalizedValue;

                OnPropertyChanged();
                NotifyOverallHealthChanged();
            }
        }

        public int PrivacyScore
        {
            get => _privacyScore;

            set
            {
                int normalizedValue =
                    NormalizeScore(value);

                if (_privacyScore == normalizedValue)
                {
                    return;
                }

                _privacyScore = normalizedValue;

                OnPropertyChanged();
                NotifyOverallHealthChanged();
            }
        }

        public int WindowsUpdateScore
        {
            get => _windowsUpdateScore;

            set
            {
                int normalizedValue =
                    NormalizeScore(value);

                if (_windowsUpdateScore == normalizedValue)
                {
                    return;
                }

                _windowsUpdateScore = normalizedValue;

                OnPropertyChanged();
                NotifyOverallHealthChanged();
            }
        }

        public int OverallHealthScore =>
            (int)Math.Round(
                (
                    PerformanceScore +
                    ServicesScore +
                    StartupScore +
                    PrivacyScore +
                    WindowsUpdateScore
                ) / 5.0);

        public string OverallHealthText =>
            $"{OverallHealthScore}%";

        public string OverallHealthStatus =>
            OverallHealthScore switch
            {
                >= 90 =>
                    LocalizationHelper.Get(
                        "SystemHealthExcellent"),

                >= 75 =>
                    LocalizationHelper.Get(
                        "SystemHealthGood"),

                >= 60 =>
                    LocalizationHelper.Get(
                        "SystemHealthAttention"),

                _ =>
                    LocalizationHelper.Get(
                        "SystemHealthCritical")
            };

        public Brush OverallHealthBrush =>
            OverallHealthScore switch
            {
                >= 90 => Brushes.LimeGreen,
                >= 75 => Brushes.Gold,
                >= 60 => Brushes.Orange,
                _ => Brushes.OrangeRed
            };

        private static int NormalizeScore(
            int score)
        {
            return Math.Clamp(
                score,
                0,
                100);
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            OnPropertyChanged(
                nameof(OverallHealthStatus));
        }

        private void NotifyOverallHealthChanged()
        {
            OnPropertyChanged(
                nameof(OverallHealthScore));

            OnPropertyChanged(
                nameof(OverallHealthText));

            OnPropertyChanged(
                nameof(OverallHealthStatus));

            OnPropertyChanged(
                nameof(OverallHealthBrush));
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