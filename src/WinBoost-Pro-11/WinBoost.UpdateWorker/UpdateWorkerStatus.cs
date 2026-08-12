namespace WinBoost.UpdateWorker
{
    internal sealed class UpdateWorkerStatus
    {
        public string State
        {
            get;
            set;
        } = "Idle";

        public int Percent
        {
            get;
            set;
        }

        public int CurrentUpdate
        {
            get;
            set;
        }

        public int TotalUpdates
        {
            get;
            set;
        }

        public string CurrentUpdateTitle
        {
            get;
            set;
        } = string.Empty;

        public string Message
        {
            get;
            set;
        } = string.Empty;

        public bool RebootRequired
        {
            get;
            set;
        }

        public bool IsCompleted
        {
            get;
            set;
        }

        public bool IsSuccessful
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        } = string.Empty;
    }
}