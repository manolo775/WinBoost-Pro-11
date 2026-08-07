using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;

namespace WinBoost.App.Controls
{
    public partial class MetricCard : UserControl
    {
        public MetricCard()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MetricCard),
                new PropertyMetadata("Metric"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(MetricCard),
                new PropertyMetadata("0%"));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(MetricCard),
                new PropertyMetadata("Normal"));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register(
                nameof(ProgressValue),
                typeof(double),
                typeof(MetricCard),
                new PropertyMetadata(
                    0.0,
                    OnProgressValueChanged));

        public double ProgressValue
        {
            get => (double)GetValue(ProgressValueProperty);
            set => SetValue(ProgressValueProperty, value);
        }

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(
                nameof(IconKind),
                typeof(PackIconKind),
                typeof(MetricCard),
                new PropertyMetadata(
                    PackIconKind.DesktopClassic));

        public PackIconKind IconKind
        {
            get => (PackIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(MetricCard),
                new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public static readonly DependencyProperty
            ShowProgressBarProperty =
            DependencyProperty.Register(
                nameof(ShowProgressBar),
                typeof(bool),
                typeof(MetricCard),
                new PropertyMetadata(true));

        public bool ShowProgressBar
        {
            get => (bool)GetValue(
                ShowProgressBarProperty);

            set => SetValue(
                ShowProgressBarProperty,
                value);
        }

        private static void OnProgressValueChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is not MetricCard card ||
                card.MetricProgressBar == null)
            {
                return;
            }

            double newValue =
                (double)eventArgs.NewValue;

            double currentValue =
                card.MetricProgressBar.Value;

            var animation =
                new DoubleAnimation
                {
                    From = currentValue,
                    To = newValue,
                    Duration =
                        new Duration(
                            System.TimeSpan.FromMilliseconds(450)),
                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                };

            card.MetricProgressBar.BeginAnimation(
                ProgressBar.ValueProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }
    }
}