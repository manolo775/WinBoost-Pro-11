using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class OptimizationLogEntry
        : INotifyPropertyChanged
    {
        private string _message =
            string.Empty;

        public DateTime Timestamp
        {
            get;
            set;
        } =
            DateTime.Now;

        public OptimizationLogLevel Level
        {
            get;
            set;
        } =
            OptimizationLogLevel.Information;

        public string ResourceKey
        {
            get;
            set;
        } =
            string.Empty;

        public string ArgumentResourceKey
        {
            get;
            set;
        } =
            string.Empty;

        public object[] ResourceArguments
        {
            get;
            set;
        } =
            Array.Empty<object>();

        public string Message
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(
                        ResourceKey))
                {
                    if (!string.IsNullOrWhiteSpace(
                            ArgumentResourceKey))
                    {
                        return LocalizationHelper.Format(
                            ResourceKey,
                            LocalizationHelper.Get(
                                ArgumentResourceKey));
                    }

                    return LocalizationHelper.Format(
                        ResourceKey,
                        ResourceArguments);
                }

                return _message;
            }

            set
            {
                _message =
                    value ?? string.Empty;
            }
        }

        public string TimeText =>
            Timestamp.ToString(
                "HH:mm:ss");

        public string IconKind =>
            Level switch
            {
                OptimizationLogLevel.Information =>
                    "InformationOutline",

                OptimizationLogLevel.Success =>
                    "CheckCircle",

                OptimizationLogLevel.Warning =>
                    "AlertOutline",

                OptimizationLogLevel.Error =>
                    "CloseCircle",

                _ =>
                    "InformationOutline"
            };

        public string IconColor =>
            Level switch
            {
                OptimizationLogLevel.Information =>
                    "#42A5F5",

                OptimizationLogLevel.Success =>
                    "#00C853",

                OptimizationLogLevel.Warning =>
                    "#FFC107",

                OptimizationLogLevel.Error =>
                    "#F44336",

                _ =>
                    "#42A5F5"
            };

        public void RefreshLocalization()
        {
            OnPropertyChanged(
                nameof(Message));
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

    public enum OptimizationLogLevel
    {
        Information,
        Success,
        Warning,
        Error
    }
}