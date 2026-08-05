using System.Collections.ObjectModel;
using System.Windows.Input;
using WinBoost.App.Commands;
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
    }
}