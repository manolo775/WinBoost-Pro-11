using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Optimization;

namespace WinBoost.App.ViewModels
{
    public sealed class OptimizationHistoryViewModel
    {
        private readonly OptimizationHistoryService
            _historyService;

        public OptimizationHistoryViewModel()
        {
            _historyService =
                OptimizationHistoryService.Instance;

            Entries =
                _historyService.Entries;

            ClearHistoryCommand =
                new RelayCommand(
                    _ =>
                        _historyService.Clear(),
                    _ =>
                        Entries.Count > 0);

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ReadOnlyObservableCollection<
            OptimizationHistoryEntry>
            Entries
        {
            get;
        }

        public ICommand ClearHistoryCommand
        {
            get;
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            foreach (OptimizationHistoryEntry entry
                     in Entries)
            {
                entry.RefreshLocalization();
            }
        }
    }
}