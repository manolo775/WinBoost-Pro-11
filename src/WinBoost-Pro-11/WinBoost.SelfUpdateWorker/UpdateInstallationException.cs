using System;

namespace WinBoost.SelfUpdateWorker
{
    internal sealed class UpdateInstallationException :
        Exception
    {
        public bool RolledBack { get; }

        public UpdateInstallationException(
            string message,
            bool rolledBack,
            Exception innerException)
            : base(
                message,
                innerException)
        {
            RolledBack =
                rolledBack;
        }
    }
}