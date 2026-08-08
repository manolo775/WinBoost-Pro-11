using System.Windows;
using System.Windows.Controls;

namespace WinBoost.App.Controls
{
    public partial class PerformanceAlertCard : UserControl
    {
        public static readonly RoutedEvent
            DetailsRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(DetailsRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PerformanceAlertCard));

        public PerformanceAlertCard()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler
            DetailsRequested
        {
            add =>
                AddHandler(
                    DetailsRequestedEvent,
                    value);

            remove =>
                RemoveHandler(
                    DetailsRequestedEvent,
                    value);
        }

        private void ViewDetailsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            RaiseEvent(
                new RoutedEventArgs(
                    DetailsRequestedEvent,
                    this));
        }
    }
}