using System.Windows;
using System.Windows.Controls;

namespace WinBoost.App.Controls
{
    public partial class HealthCard : UserControl
    {
        public static readonly RoutedEvent
            PerformanceRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PerformanceRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HealthCard));

        public static readonly RoutedEvent
            ServicesRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ServicesRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HealthCard));

        public static readonly RoutedEvent
            StartupRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(StartupRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HealthCard));

        public static readonly RoutedEvent
            PrivacyRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PrivacyRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HealthCard));

        public static readonly RoutedEvent
            WindowsUpdateRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(WindowsUpdateRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HealthCard));

        public HealthCard()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler
            PerformanceRequested
        {
            add =>
                AddHandler(
                    PerformanceRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    PerformanceRequestedEvent,
                    value);
        }

        public event RoutedEventHandler
            ServicesRequested
        {
            add =>
                AddHandler(
                    ServicesRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    ServicesRequestedEvent,
                    value);
        }

        public event RoutedEventHandler
            StartupRequested
        {
            add =>
                AddHandler(
                    StartupRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    StartupRequestedEvent,
                    value);
        }

        public event RoutedEventHandler
            PrivacyRequested
        {
            add =>
                AddHandler(
                    PrivacyRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    PrivacyRequestedEvent,
                    value);
        }

        public event RoutedEventHandler
            WindowsUpdateRequested
        {
            add =>
                AddHandler(
                    WindowsUpdateRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    WindowsUpdateRequestedEvent,
                    value);
        }

        private void PerformanceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    PerformanceRequestedEvent,
                    this));
        }

        private void ServicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    ServicesRequestedEvent,
                    this));
        }

        private void StartupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    StartupRequestedEvent,
                    this));
        }

        private void PrivacyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    PrivacyRequestedEvent,
                    this));
        }

        private void WindowsUpdateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    WindowsUpdateRequestedEvent,
                    this));
        }
    }
}