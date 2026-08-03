using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Privacy;

namespace WinBoost.App.ViewModels
{
    public sealed class PrivacyViewModel : INotifyPropertyChanged
    {
        private readonly PrivacyScanService
            _privacyScanService;

        private readonly PrivacyRecommendationEngine
            _privacyRecommendationEngine;

        private readonly SystemHealthStateService
            _healthStateService;

        private bool _isScanning;

        private string _scanStatus =
            "Pornește o scanare pentru a verifica setările de confidențialitate.";

        private string _overallStatus =
            "Neverificat";

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

                _scanStatus = value;
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

                _overallStatus = value;
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

                _isScanning = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ScanButtonText));

                CommandManager.InvalidateRequerySuggested();
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
                ? "Se scanează..."
                : "Scan Privacy";

        public PrivacyViewModel()
        {
            _privacyScanService =
                new PrivacyScanService();

            _privacyRecommendationEngine =
                new PrivacyRecommendationEngine();

            _healthStateService =
                SystemHealthStateService.Instance;

            PrivacyItems =
                new ObservableCollection<PrivacyCheckItem>
                {
                    new PrivacyCheckItem
                    {
                        Id = "diagnostic-data",

                        Title =
                            "Diagnostic și telemetrie",

                        Description =
                            "Verifică nivelul datelor de diagnostic " +
                            "trimise către Microsoft."
                    },

                    new PrivacyCheckItem
                    {
                        Id = "advertising-id",

                        Title =
                            "Advertising ID",

                        Description =
                            "Verifică utilizarea identificatorului " +
                            "pentru reclame personalizate."
                    },

                    new PrivacyCheckItem
                    {
                        Id = "activity-history",

                        Title =
                            "Activity History",

                        Description =
                            "Verifică dacă Windows salvează " +
                            "istoricul activităților."
                    },

                    new PrivacyCheckItem
                    {
                        Id = "location-services",

                        Title =
                            "Location Services",

                        Description =
                            "Verifică accesul aplicațiilor la " +
                            "locația dispozitivului."
                    }
                };

            Recommendations =
                new ObservableCollection<HealthRecommendation>();

            ScanPrivacyCommand =
                new RelayCommand(
                    async _ =>
                        await ScanPrivacyAsync(),
                    _ =>
                        !IsScanning);
        }

        private async Task ScanPrivacyAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;

            OverallStatus =
                "Se verifică";

            ScanStatus =
                "Se verifică setările de confidențialitate...";

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
                    PrivacyItems.Add(item);
                }

                BuildRecommendations();
                UpdatePrivacyHealthScore();

                OverallStatus =
                    "Verificat";

                UpdateScanStatusMessage();
            }
            catch (Exception ex)
            {
                OverallStatus =
                    "Eroare";

                ScanStatus =
                    "Scanarea nu a putut fi finalizată: " +
                    ex.Message;
            }
            finally
            {
                IsScanning = false;
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

        private void UpdateScanStatusMessage()
        {
            if (RecommendationCount == 0)
            {
                ScanStatus =
                    "🟢 Nu au fost găsite probleme de " +
                    "confidențialitate. PC-ul tău este " +
                    "configurat corespunzător.";

                return;
            }

            if (RecommendationCount == 1)
            {
                ScanStatus =
                    "🟢 A fost găsită o recomandare pentru " +
                    "îmbunătățirea confidențialității.";

                return;
            }

            ScanStatus =
                $"🟢 Au fost găsite {RecommendationCount} " +
                "recomandări pentru îmbunătățirea " +
                "confidențialității.";
        }

        private void UpdatePrivacyHealthScore()
        {
            if (PrivacyItems.Count == 0)
            {
                _healthStateService.UpdatePrivacyData(
                    0,
                    0);

                return;
            }

            double totalPoints =
                PrivacyItems.Sum(
                    item =>
                        item.StatusLevel switch
                        {
                            "Good" => 100,
                            "Neutral" => 70,
                            "Attention" => 40,
                            "Critical" => 0,
                            _ => 70
                        });

            int weightedScore =
                (int)Math.Round(
                    totalPoints /
                    PrivacyItems.Count);

            _healthStateService.UpdatePrivacyData(
                100,
                weightedScore);
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