using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WinBoost.App.Models
{
    public class WindowsServiceInfo : INotifyPropertyChanged
    {
        private string _status =
            string.Empty;

        private Brush _statusBrush =
            Brushes.LightGray;

        private bool _isBusy;

        private bool _isCritical;

        private bool _canBeStoppedSafely =
            true;

        private string _riskLevel =
            "Unknown";

        private string _startType =
            string.Empty;

        private string _selectedStartupType =
            string.Empty;

        public WindowsServiceInfo()
        {
            AvailableStartupTypes =
                new ObservableCollection<string>
                {
                    "Automatic",
                    "Automatic (Delayed)",
                    "Manual",
                    "Disabled"
                };
        }

        public string DisplayName { get; set; } =
            string.Empty;

        public string ServiceName { get; set; } =
            string.Empty;

        public string Recommendation { get; set; } =
            string.Empty;

        public ObservableCollection<string>
            AvailableStartupTypes
        {
            get;
        }

        public string Status
        {
            get => _status;

            set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanRestart));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;

            set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged();
            }
        }

        public string StartType
        {
            get => _startType;

            set
            {
                if (_startType == value)
                {
                    return;
                }

                _startType = value;

                OnPropertyChanged();

                SelectedStartupType = value;
            }
        }

        public string SelectedStartupType
        {
            get => _selectedStartupType;

            set
            {
                if (_selectedStartupType == value)
                {
                    return;
                }

                _selectedStartupType = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStartupTypeChanged));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanRestart));
                OnPropertyChanged(nameof(CanChangeStartupType));
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

                _isCritical = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanRestart));
                OnPropertyChanged(nameof(CanChangeStartupType));
            }
        }

        public bool CanBeStoppedSafely
        {
            get => _canBeStoppedSafely;

            set
            {
                if (_canBeStoppedSafely == value)
                {
                    return;
                }

                _canBeStoppedSafely = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanRestart));
            }
        }

        public string RiskLevel
        {
            get => _riskLevel;

            set
            {
                if (_riskLevel == value)
                {
                    return;
                }

                _riskLevel = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(RiskBrush));
            }
        }

        public Brush RiskBrush =>
            RiskLevel switch
            {
                "Critical" => Brushes.OrangeRed,
                "High" => Brushes.Orange,
                "Medium" => Brushes.Gold,
                "Low" => Brushes.LimeGreen,
                _ => Brushes.LightGray
            };

        public bool CanStart =>
            !IsBusy &&
            !Status.Equals(
                "Running",
                StringComparison.OrdinalIgnoreCase) &&
            !StartType.Equals(
                "Disabled",
                StringComparison.OrdinalIgnoreCase);

        public bool CanStop =>
            !IsBusy &&
            !IsCritical &&
            CanBeStoppedSafely &&
            Status.Equals(
                "Running",
                StringComparison.OrdinalIgnoreCase);

        public bool CanRestart =>
            !IsBusy &&
            !IsCritical &&
            CanBeStoppedSafely &&
            Status.Equals(
                "Running",
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

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}