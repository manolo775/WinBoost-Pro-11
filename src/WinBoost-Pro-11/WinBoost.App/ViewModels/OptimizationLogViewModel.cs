using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Optimization;

namespace WinBoost.App.ViewModels
{
    public sealed class OptimizationLogViewModel
    {
        private readonly OptimizationLogService
            _logService;

        public OptimizationLogViewModel()
        {
            _logService =
                OptimizationLogService.Instance;

            Entries =
                _logService.Entries;

            ClearLogCommand =
                new RelayCommand(
                    _ =>
                        _logService.Clear(),
                    _ =>
                        Entries.Count > 0);

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ReadOnlyObservableCollection<
            OptimizationLogEntry>
            Entries
        {
            get;
        }

        public ICommand ClearLogCommand
        {
            get;
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            foreach (OptimizationLogEntry entry
                     in Entries)
            {
                entry.RefreshLocalization();
            }
        }
    }
}