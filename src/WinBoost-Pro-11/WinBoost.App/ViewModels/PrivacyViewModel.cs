using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Privacy;

namespace WinBoost.App.ViewModels
{
    public sealed class PrivacyViewModel :
        INotifyPropertyChanged
    {
        private readonly PrivacyScanService
            _privacyScanService;

        private readonly PrivacyRecommendationEngine
            _privacyRecommendationEngine;

        private readonly SystemHealthStateService
            _healthStateService;

        private bool _isScanning;

        private PrivacyUiState _currentUiState =
            PrivacyUiState.NotScanned;

        private string _scanStatus =
            string.Empty;

        private string _overallStatus =
            string.Empty;

        private string _lastErrorMessage =
            string.Empty;

        private bool _hasCompletedScan;

        public PrivacyViewModel()
        {
            _privacyScanService =
                new PrivacyScanService();

            _privacyRecommendationEngine =
                new PrivacyRecommendationEngine();

            _healthStateService =
                SystemHealthStateService.Instance;

            PrivacyItems =
                CreateInitialPrivacyItems();

            Recommendations =
                new ObservableCollection<
                    HealthRecommendation>();

            ScanPrivacyCommand =
                new RelayCommand(
                    async _ =>
                        await ScanPrivacyAsync(),
                    _ =>
                        !IsScanning);

            LanguageManager
                .Instance
                .LanguageChanged +=
                    LanguageManager_LanguageChanged;

            ApplyLocalizedUiState();
        }

        public ObservableCollection<PrivacyCheckItem>
            PrivacyItems
        {
            get;
        }

        public ObservableCollection<HealthRecommendation>
            Recommendations
        {
            get;
        }

        public ICommand ScanPrivacyCommand
        {
            get;
        }

        public string ScanStatus
        {
            get => _scanStatus;

            private set
            {
                if (_scanStatus == value)
                {
                    return;
                }

                _scanStatus =
                    value;

                OnPropertyChanged();
            }
        }

        public string OverallStatus
        {
            get => _overallStatus;

            private set
            {
                if (_overallStatus == value)
                {
                    return;
                }

                _overallStatus =
                    value;

                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;

            private set
            {
                if (_isScanning == value)
                {
                    return;
                }

                _isScanning =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(ScanButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public int RecommendationCount =>
            Recommendations.Count(
                recommendation =>
                    recommendation.PotentialGain > 0);

        public int PotentialGain =>
            Recommendations.Sum(
                recommendation =>
                    recommendation.PotentialGain);

        public string ScanButtonText =>
            IsScanning
                ? LocalizationHelper.Get(
                    "PrivacyButtonScanning")
                : LocalizationHelper.Get(
                    "PrivacyButtonScan");

        private static ObservableCollection<PrivacyCheckItem>
            CreateInitialPrivacyItems()
        {
            return new ObservableCollection<PrivacyCheckItem>
            {
                new()
                {
                    Id =
                        "diagnostic-data",

                    TitleResourceKey =
                        "PrivacyItemDiagnosticTitle",

                    DescriptionResourceKey =
                        "PrivacyItemDiagnosticDescription",

                    StatusResourceKey =
                        "PrivacyStatusNotScanned"
                },

                new()
                {
                    Id =
                        "advertising-id",

                    TitleResourceKey =
                        "PrivacyItemAdvertisingTitle",

                    DescriptionResourceKey =
                        "PrivacyItemAdvertisingDescription",

                    StatusResourceKey =
                        "PrivacyStatusNotScanned"
                },

                new()
                {
                    Id =
                        "activity-history",

                    TitleResourceKey =
                        "PrivacyItemActivityHistoryTitle",

                    DescriptionResourceKey =
                        "PrivacyItemActivityHistoryDescription",

                    StatusResourceKey =
                        "PrivacyStatusNotScanned"
                },

                new()
                {
                    Id =
                        "location-services",

                    TitleResourceKey =
                        "PrivacyItemLocationTitle",

                    DescriptionResourceKey =
                        "PrivacyItemLocationDescription",

                    StatusResourceKey =
                        "PrivacyStatusNotScanned"
                }
            };
        }

        private async Task ScanPrivacyAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning =
                true;

            SetUiState(
                PrivacyUiState.Scanning);

            try
            {
                IReadOnlyList<PrivacyCheckItem> results =
                    await Task.Run(
                        () =>
                            _privacyScanService.Scan());

                PrivacyItems.Clear();

                foreach (PrivacyCheckItem item
                         in results)
                {
                    PrivacyItems.Add(
                        item);
                }

                _hasCompletedScan =
                    true;

                BuildRecommendations();

                UpdatePrivacyHealthScore();

                SetUiState(
                    RecommendationCount switch
                    {
                        0 =>
                            PrivacyUiState.CompletedNoIssues,

                        1 =>
                            PrivacyUiState.CompletedOneRecommendation,

                        _ =>
                            PrivacyUiState.CompletedMultipleRecommendations
                    });
            }
            catch (Exception ex)
            {
                _lastErrorMessage =
                    ex.Message;

                SetUiState(
                    PrivacyUiState.Error);
            }
            finally
            {
                IsScanning =
                    false;

                OnPropertyChanged(
                    nameof(ScanButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private void BuildRecommendations()
        {
            Recommendations.Clear();

            IReadOnlyList<HealthRecommendation>
                recommendations =
                    _privacyRecommendationEngine
                        .BuildRecommendations(
                            PrivacyItems);

            foreach (HealthRecommendation recommendation
                     in recommendations)
            {
                Recommendations.Add(
                    recommendation);
            }

            OnPropertyChanged(
                nameof(RecommendationCount));

            OnPropertyChanged(
                nameof(PotentialGain));
        }

        private void UpdatePrivacyHealthScore()
        {
            if (PrivacyItems.Count == 0)
            {
                _healthStateService
                    .UpdatePrivacyData(
                        0,
                        0);

                WinBoostHealthScoreService
                    .Instance
                    .PrivacyScore =
                        0;

                return;
            }

            double totalPoints =
                PrivacyItems.Sum(
                    item =>
                        item.StatusLevel switch
                        {
                            "Good" =>
                                100,

                            "Neutral" =>
                                70,

                            "Attention" =>
                                40,

                            "Critical" =>
                                0,

                            _ =>
                                70
                        });

            int weightedScore =
                (int)Math.Round(
                    totalPoints /
                    PrivacyItems.Count);

            _healthStateService
                .UpdatePrivacyData(
                    100,
                    weightedScore);

            WinBoostHealthScoreService
                .Instance
                .PrivacyScore =
                    weightedScore;
        }

        private void SetUiState(
            PrivacyUiState state)
        {
            _currentUiState =
                state;

            ApplyLocalizedUiState();
        }

        private void ApplyLocalizedUiState()
        {
            switch (_currentUiState)
            {
                case PrivacyUiState.NotScanned:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusNotScanned");

                    ScanStatus =
                        LocalizationHelper.Get(
                            "PrivacyScanInitialMessage");

                    break;

                case PrivacyUiState.Scanning:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusScanning");

                    ScanStatus =
                        LocalizationHelper.Get(
                            "PrivacyScanCheckingMessage");

                    break;

                case PrivacyUiState.CompletedNoIssues:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusScanned");

                    ScanStatus =
                        LocalizationHelper.Get(
                            "PrivacyScanNoIssues");

                    break;

                case PrivacyUiState.CompletedOneRecommendation:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusScanned");

                    ScanStatus =
                        LocalizationHelper.Get(
                            "PrivacyScanOneRecommendation");

                    break;

                case PrivacyUiState.CompletedMultipleRecommendations:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusScanned");

                    ScanStatus =
                        LocalizationHelper.Format(
                            "PrivacyScanMultipleRecommendationsFormat",
                            RecommendationCount);

                    break;

                case PrivacyUiState.Error:

                    OverallStatus =
                        LocalizationHelper.Get(
                            "PrivacyStatusError");

                    ScanStatus =
                        LocalizationHelper.Format(
                            "PrivacyScanFailedFormat",
                            _lastErrorMessage);

                    break;
            }
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            foreach (PrivacyCheckItem item
                     in PrivacyItems)
            {
                item.RefreshLocalizedProperties();
            }

            if (_hasCompletedScan)
            {
                BuildRecommendations();
            }

            OnPropertyChanged(
                nameof(ScanButtonText));

            ApplyLocalizedUiState();

            CommandManager
                .InvalidateRequerySuggested();
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

        private enum PrivacyUiState
        {
            NotScanned,
            Scanning,
            CompletedNoIssues,
            CompletedOneRecommendation,
            CompletedMultipleRecommendations,
            Error
        }
    }
}