using System;

namespace WinBoost.App.Models
{
    public sealed class WinBoostSelfUpdateResult
    {
        public bool Success
        {
            get;
            set;
        }

        public bool RolledBack
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        } = string.Empty;

        public DateTime CompletedAtUtc
        {
            get;
            set;
        }
    }
}