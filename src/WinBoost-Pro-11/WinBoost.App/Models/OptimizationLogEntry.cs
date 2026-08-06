using System;

namespace WinBoost.App.Models
{
    public sealed class OptimizationLogEntry
    {
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

        public string Message
        {
            get;
            set;
        } =
            string.Empty;

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
    }

    public enum OptimizationLogLevel
    {
        Information,
        Success,
        Warning,
        Error
    }
}