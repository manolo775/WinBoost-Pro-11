using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class WindowsServiceInfo :
        INotifyPropertyChanged
    {
        public const string StatusRunning =
            "Running";

        public const string StatusStopped =
            "Stopped";

        public const string StartupAutomatic =
            "Automatic";

        public const string StartupAutomaticDelayed =
            "Automatic (Delayed)";

        public const string StartupManual =
            "Manual";

        public const string StartupDisabled =
            "Disabled";

        public const string RiskCritical =
            "Critical";

        public const string RiskHigh =
            "High";

        public const string RiskMedium =
            "Medium";

        public const string RiskLow =
            "Low";

        public const string RiskUnknown =
            "Unknown";

        private string _status =
            StatusStopped;

        private Brush _statusBrush =
            Brushes.LightGray;

        private bool _isBusy;

        private bool _isCritical;

        private bool _canBeStoppedSafely =
            true;

        private string _riskLevel =
            RiskUnknown;

        private string _startType =
            StartupManual;

        private string _selectedStartupType =
            StartupManual;

        private string _recommendation =
            string.Empty;

        public WindowsServiceInfo()
        {
            AvailableStartupTypes =
                new ObservableCollection<string>();

            RebuildAvailableStartupTypes();
        }

        public string DisplayName
        {
            get;
            set;
        } =
            string.Empty;

        public string ServiceName
        {
            get;
            set;
        } =
            string.Empty;

        /*
         * Valoarea internă a recomandării.
         * Este păstrată în formatul folosit de scanner.
         */
        public string Recommendation
        {
            get => _recommendation;

            set
            {
                string normalizedValue =
                    value ?? string.Empty;

                if (_recommendation ==
                    normalizedValue)
                {
                    return;
                }

                _recommendation =
                    normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(RecommendationText));

                OnPropertyChanged(
                    nameof(RecommendationBrush));
            }
        }

        public ObservableCollection<string>
            AvailableStartupTypes
        {
            get;
        }

        /*
         * Valoare internă:
         * Running / Stopped.
         */
        public string Status
        {
            get => _status;

            set
            {
                string normalizedValue =
                    NormalizeStatus(
                        value);

                if (_status ==
                    normalizedValue)
                {
                    return;
                }

                _status =
                    normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(StatusText));

                OnPropertyChanged(
                    nameof(CanStart));

                OnPropertyChanged(
                    nameof(CanStop));

                OnPropertyChanged(
                    nameof(CanRestart));
            }
        }

        /*
         * Textul afișat în interfață.
         */
        public string StatusText =>
            Status.Equals(
                StatusRunning,
                StringComparison.OrdinalIgnoreCase)
                ? LocalizationHelper.Get(
                    "ServicesServiceStatusRunning")
                : LocalizationHelper.Get(
                    "ServicesServiceStatusStopped");

        public Brush StatusBrush
        {
            get => _statusBrush;

            set
            {
                if (Equals(
                        _statusBrush,
                        value))
                {
                    return;
                }

                _statusBrush =
                    value;

                OnPropertyChanged();
            }
        }

        /*
         * Valoare internă:
         * Automatic / Automatic (Delayed) /
         * Manual / Disabled.
         */
        public string StartType
        {
            get => _startType;

            set
            {
                string normalizedValue =
                    NormalizeStartupType(
                        value);

                if (_startType ==
                    normalizedValue)
                {
                    return;
                }

                _startType =
                    normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(StartTypeText));

                SelectedStartupType =
                    normalizedValue;
            }
        }

        public string StartTypeText =>
            GetStartupTypeDisplayText(
                StartType);

        /*
         * Pentru moment păstrăm valoarea internă,
         * deoarece este folosită de comenzile existente.
         */
        public string SelectedStartupType
        {
            get => _selectedStartupType;

            set
            {
                string normalizedValue =
                    NormalizeStartupType(
                        value);

                if (_selectedStartupType ==
                    normalizedValue)
                {
                    return;
                }

                _selectedStartupType =
                    normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(SelectedStartupTypeText));

                OnPropertyChanged(
                    nameof(HasStartupTypeChanged));
            }
        }

        public string SelectedStartupTypeText =>
            GetStartupTypeDisplayText(
                SelectedStartupType);

        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanStart));

                OnPropertyChanged(
                    nameof(CanStop));

                OnPropertyChanged(
                    nameof(CanRestart));

                OnPropertyChanged(
                    nameof(CanChangeStartupType));
            }
        }

        public bool IsCritical
        {
            get => _isCritical;

            set
            {
                if (_isCritical == value)
                {
                    return;
                }

                _isCritical =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanStart));

                OnPropertyChanged(
                    nameof(CanStop));

                OnPropertyChanged(
                    nameof(CanRestart));

                OnPropertyChanged(
                    nameof(CanChangeStartupType));
            }
        }

        public bool CanBeStoppedSafely
        {
            get => _canBeStoppedSafely;

            set
            {
                if (_canBeStoppedSafely ==
                    value)
                {
                    return;
                }

                _canBeStoppedSafely =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanStop));

                OnPropertyChanged(
                    nameof(CanRestart));
            }
        }

        /*
         * Valoare internă:
         * Critical / High / Medium / Low / Unknown.
         */
        public string RiskLevel
        {
            get => _riskLevel;

            set
            {
                string normalizedValue =
                    NormalizeRiskLevel(
                        value);

                if (_riskLevel ==
                    normalizedValue)
                {
                    return;
                }

                _riskLevel =
                    normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(RiskLevelText));

                OnPropertyChanged(
                    nameof(RiskBrush));

                OnPropertyChanged(
                    nameof(HealthScore));

                OnPropertyChanged(
                    nameof(HealthScoreText));

                OnPropertyChanged(
                    nameof(HealthScoreBrush));
            }
        }

        public string RiskLevelText =>
            RiskLevel switch
            {
                RiskCritical =>
                    LocalizationHelper.Get(
                        "ServicesRiskCritical"),

                RiskHigh =>
                    LocalizationHelper.Get(
                        "ServicesRiskHigh"),

                RiskMedium =>
                    LocalizationHelper.Get(
                        "ServicesRiskMedium"),

                RiskLow =>
                    LocalizationHelper.Get(
                        "ServicesRiskLow"),

                _ =>
                    LocalizationHelper.Get(
                        "ServicesRiskUnknown")
            };

        public Brush RiskBrush =>
            RiskLevel switch
            {
                RiskCritical =>
                    Brushes.OrangeRed,

                RiskHigh =>
                    Brushes.Orange,

                RiskMedium =>
                    Brushes.Gold,

                RiskLow =>
                    Brushes.LimeGreen,

                _ =>
                    Brushes.LightGray
            };

        public string RecommendationText =>
            Recommendation switch
            {
                "Critical service" =>
                    LocalizationHelper.Get(
                        "ServicesRecommendationCritical"),

                "Keep enabled" =>
                    LocalizationHelper.Get(
                        "ServicesRecommendationKeepEnabled"),

                "Optional" =>
                    LocalizationHelper.Get(
                        "ServicesRecommendationOptional"),

                "Safe to disable if unused" =>
                    LocalizationHelper.Get(
                        "ServicesRecommendationSafeToDisable"),

                "Review" =>
                    LocalizationHelper.Get(
                        "ServicesRecommendationReview"),

                _ =>
                    Recommendation
            };

        public Brush RecommendationBrush =>
            Recommendation switch
            {
                "Critical service" =>
                    Brushes.OrangeRed,

                "Keep enabled" =>
                    Brushes.LimeGreen,

                "Optional" =>
                    Brushes.Gold,

                "Safe to disable if unused" =>
                    Brushes.DeepSkyBlue,

                "Review" =>
                    Brushes.Silver,

                _ =>
                    Brushes.LightGray
            };

        public int HealthScore =>
            RiskLevel switch
            {
                RiskCritical =>
                    100,

                RiskHigh =>
                    90,

                RiskMedium =>
                    75,

                RiskLow =>
                    60,

                _ =>
                    70
            };

        public Brush HealthScoreBrush =>
            HealthScore switch
            {
                >= 90 =>
                    Brushes.LimeGreen,

                >= 75 =>
                    Brushes.Gold,

                >= 60 =>
                    Brushes.Orange,

                _ =>
                    Brushes.OrangeRed
            };

        public string HealthScoreText =>
            $"{HealthScore}%";

        public bool CanStart =>
            !IsBusy &&
            !Status.Equals(
                StatusRunning,
                StringComparison.OrdinalIgnoreCase) &&
            !StartType.Equals(
                StartupDisabled,
                StringComparison.OrdinalIgnoreCase);

        public bool CanStop =>
            !IsBusy &&
            !IsCritical &&
            CanBeStoppedSafely &&
            Status.Equals(
                StatusRunning,
                StringComparison.OrdinalIgnoreCase);

        public bool CanRestart =>
            !IsBusy &&
            !IsCritical &&
            CanBeStoppedSafely &&
            Status.Equals(
                StatusRunning,
                StringComparison.OrdinalIgnoreCase);

        public bool CanChangeStartupType =>
            !IsBusy &&
            !IsCritical;

        public bool HasStartupTypeChanged =>
            !string.Equals(
                StartType,
                SelectedStartupType,
                StringComparison.OrdinalIgnoreCase);

        public void ConfirmStartupTypeChange()
        {
            StartType =
                SelectedStartupType;
        }

        public void CancelStartupTypeChange()
        {
            SelectedStartupType =
                StartType;
        }

        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(
                nameof(StatusText));

            OnPropertyChanged(
                nameof(StartTypeText));

            OnPropertyChanged(
                nameof(SelectedStartupTypeText));

            OnPropertyChanged(
                nameof(RiskLevelText));

            OnPropertyChanged(
                nameof(RecommendationText));

            RebuildAvailableStartupTypes();
        }

        private void RebuildAvailableStartupTypes()
        {
            AvailableStartupTypes.Clear();

            /*
             * Păstrăm momentan valorile interne pentru
             * compatibilitate cu ServiceStartupTypeViewModel.
             * Localizarea ComboBox-ului va fi făcută
             * după verificarea acelui ViewModel.
             */
            AvailableStartupTypes.Add(
                StartupAutomatic);

            AvailableStartupTypes.Add(
                StartupAutomaticDelayed);

            AvailableStartupTypes.Add(
                StartupManual);

            AvailableStartupTypes.Add(
                StartupDisabled);
        }

        private static string GetStartupTypeDisplayText(
            string startupType)
        {
            return startupType switch
            {
                StartupAutomatic =>
                    LocalizationHelper.Get(
                        "ServicesStartupAutomatic"),

                StartupAutomaticDelayed =>
                    LocalizationHelper.Get(
                        "ServicesStartupAutomaticDelayed"),

                StartupManual =>
                    LocalizationHelper.Get(
                        "ServicesStartupManual"),

                StartupDisabled =>
                    LocalizationHelper.Get(
                        "ServicesStartupDisabled"),

                _ =>
                    startupType
            };
        }

        private static string NormalizeStatus(
            string? status)
        {
            if (string.Equals(
                    status,
                    StatusRunning,
                    StringComparison.OrdinalIgnoreCase))
            {
                return StatusRunning;
            }

            return StatusStopped;
        }

        private static string NormalizeStartupType(
            string? startupType)
        {
            if (string.Equals(
                    startupType,
                    StartupAutomatic,
                    StringComparison.OrdinalIgnoreCase))
            {
                return StartupAutomatic;
            }

            if (string.Equals(
                    startupType,
                    StartupAutomaticDelayed,
                    StringComparison.OrdinalIgnoreCase))
            {
                return StartupAutomaticDelayed;
            }

            if (string.Equals(
                    startupType,
                    StartupDisabled,
                    StringComparison.OrdinalIgnoreCase))
            {
                return StartupDisabled;
            }

            return StartupManual;
        }

        private static string NormalizeRiskLevel(
            string? riskLevel)
        {
            if (string.Equals(
                    riskLevel,
                    RiskCritical,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RiskCritical;
            }

            if (string.Equals(
                    riskLevel,
                    RiskHigh,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RiskHigh;
            }

            if (string.Equals(
                    riskLevel,
                    RiskMedium,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RiskMedium;
            }

            if (string.Equals(
                    riskLevel,
                    RiskLow,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RiskLow;
            }

            return RiskUnknown;
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

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