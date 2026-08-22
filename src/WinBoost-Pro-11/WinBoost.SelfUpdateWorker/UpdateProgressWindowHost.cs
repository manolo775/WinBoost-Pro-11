using System;
using System.Threading;
using System.Windows.Threading;

namespace WinBoost.SelfUpdateWorker
{
    internal sealed class UpdateProgressWindowHost :
        IDisposable
    {
        private readonly object _syncRoot =
            new object();

        private readonly ManualResetEventSlim _readyEvent =
            new ManualResetEventSlim(false);

        private Thread? _uiThread;

        private UpdateProgressWindow? _window;

        private Exception? _startupException;

        public bool Start()
        {
            lock (_syncRoot)
            {
                if (_uiThread != null &&
                    _uiThread.IsAlive)
                {
                    return true;
                }

                _startupException =
                    null;

                _readyEvent.Reset();

                _uiThread =
                    new Thread(
                        RunWindow);

                _uiThread.Name =
                    "WinBoost Self Update UI";

                _uiThread.IsBackground =
                    true;

                _uiThread.SetApartmentState(
                    ApartmentState.STA);

                _uiThread.Start();
            }

            if (!_readyEvent.Wait(
                    TimeSpan.FromSeconds(5)))
            {
                UpdateLogger.Write(
                    "Update progress window startup timed out.");

                return false;
            }

            if (_startupException != null)
            {
                UpdateLogger.WriteException(
                    "Update progress window could not be started",
                    _startupException);

                return false;
            }

            return _window != null;
        }

        public void UpdateProgress(
            string statusText,
            double progressValue)
        {
            UpdateProgressWindow? window =
                _window;

            if (window == null)
            {
                return;
            }

            try
            {
                window.Dispatcher.BeginInvoke(
                    () =>
                        window.UpdateProgress(
                            statusText,
                            progressValue));
            }
            catch (Exception ex)
            {
                UpdateLogger.WriteException(
                    "Could not update progress window",
                    ex);
            }
        }

        public void UpdateStatus(
            string statusText)
        {
            UpdateProgressWindow? window =
                _window;

            if (window == null)
            {
                return;
            }

            try
            {
                window.Dispatcher.BeginInvoke(
                    () =>
                        window.UpdateStatus(
                            statusText));
            }
            catch (Exception ex)
            {
                UpdateLogger.WriteException(
                    "Could not update progress window status",
                    ex);
            }
        }

        public void Close()
        {
            UpdateProgressWindow? window =
                _window;

            if (window == null)
            {
                return;
            }

            try
            {
                window.Dispatcher.BeginInvoke(
                    () =>
                    {
                        if (window.IsVisible)
                        {
                            window.Close();
                        }
                    });
            }
            catch (Exception ex)
            {
                UpdateLogger.WriteException(
                    "Could not close progress window",
                    ex);
            }
        }

        public void Dispose()
        {
            Close();

            _readyEvent.Dispose();
        }

        private void RunWindow()
        {
            try
            {
                UpdateProgressWindow window =
                    new UpdateProgressWindow();

                _window =
                    window;

                window.Closed +=
                    (_, _) =>
                    {
                        _window =
                            null;

                        Dispatcher
                            .CurrentDispatcher
                            .BeginInvokeShutdown(
                                DispatcherPriority.Background);
                    };

                window.Show();

                _readyEvent.Set();

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _startupException =
                    ex;

                _readyEvent.Set();

                UpdateLogger.WriteException(
                    "Update progress UI thread failed",
                    ex);
            }
        }
    }
}