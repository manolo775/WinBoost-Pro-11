using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;

namespace WinBoost.App.Controls
{
    public partial class MetricCard : UserControl
    {
        public MetricCard()
        {
            InitializeComponent();

            UpdateInteractiveState(
                IsClickable);
        }

        public static readonly DependencyProperty
            TitleProperty =
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

        public static readonly DependencyProperty
            ValueProperty =
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

        public static readonly DependencyProperty
            StatusProperty =
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

        public static readonly DependencyProperty
            ProgressValueProperty =
            DependencyProperty.Register(
                nameof(ProgressValue),
                typeof(double),
                typeof(MetricCard),
                new PropertyMetadata(
                    0.0,
                    OnProgressValueChanged));

        public double ProgressValue
        {
            get =>
                (double)GetValue(
                    ProgressValueProperty);

            set =>
                SetValue(
                    ProgressValueProperty,
                    value);
        }

        public static readonly DependencyProperty
            IconKindProperty =
            DependencyProperty.Register(
                nameof(IconKind),
                typeof(PackIconKind),
                typeof(MetricCard),
                new PropertyMetadata(
                    PackIconKind.DesktopClassic));

        public PackIconKind IconKind
        {
            get =>
                (PackIconKind)GetValue(
                    IconKindProperty);

            set =>
                SetValue(
                    IconKindProperty,
                    value);
        }

        public static readonly DependencyProperty
            SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(MetricCard),
                new PropertyMetadata(
                    string.Empty));

        public string Subtitle
        {
            get =>
                (string)GetValue(
                    SubtitleProperty);

            set =>
                SetValue(
                    SubtitleProperty,
                    value);
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
            get =>
                (bool)GetValue(
                    ShowProgressBarProperty);

            set =>
                SetValue(
                    ShowProgressBarProperty,
                    value);
        }

        public static readonly DependencyProperty
            IsClickableProperty =
            DependencyProperty.Register(
                nameof(IsClickable),
                typeof(bool),
                typeof(MetricCard),
                new PropertyMetadata(
                    false,
                    OnIsClickableChanged));

        public bool IsClickable
        {
            get =>
                (bool)GetValue(
                    IsClickableProperty);

            set =>
                SetValue(
                    IsClickableProperty,
                    value);
        }

        public static readonly RoutedEvent
            ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MetricCard));

        public event RoutedEventHandler Click
        {
            add =>
                AddHandler(
                    ClickEvent,
                    value);

            remove =>
                RemoveHandler(
                    ClickEvent,
                    value);
        }

        private static void OnProgressValueChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs
                eventArgs)
        {
            if (dependencyObject
                    is not MetricCard card ||
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
                    From =
                        currentValue,

                    To =
                        newValue,

                    Duration =
                        new Duration(
                            TimeSpan
                                .FromMilliseconds(
                                    450)),

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            card.MetricProgressBar
                .BeginAnimation(
                    ProgressBar.ValueProperty,
                    animation,
                    HandoffBehavior
                        .SnapshotAndReplace);
        }

        private static void OnIsClickableChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs
                eventArgs)
        {
            if (dependencyObject
                is not MetricCard card)
            {
                return;
            }

            card.UpdateInteractiveState(
                (bool)eventArgs.NewValue);
        }

        private void UpdateInteractiveState(
            bool isClickable)
        {
            Focusable =
                isClickable;

            Cursor =
                isClickable
                    ? Cursors.Hand
                    : Cursors.Arrow;

            KeyboardNavigation.SetIsTabStop(
                this,
                isClickable);
        }

        private void
            MetricCard_PreviewMouseLeftButtonDown(
                object sender,
                MouseButtonEventArgs e)
        {
            if (!IsClickable)
            {
                return;
            }

            Focus();
        }

        private void MetricCard_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!IsClickable)
            {
                return;
            }

            RaiseClick();

            e.Handled =
                true;
        }

        private void MetricCard_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (!IsClickable ||
                e.IsRepeat)
            {
                return;
            }

            if (e.Key != Key.Enter &&
                e.Key != Key.Space)
            {
                return;
            }

            RaiseClick();

            e.Handled =
                true;
        }

        private void RaiseClick()
        {
            PlayClickAnimation();

            RaiseEvent(
                new RoutedEventArgs(
                    ClickEvent,
                    this));
        }

        private void PlayClickAnimation()
        {
            var animation =
                new DoubleAnimation
                {
                    To =
                        0.72,

                    Duration =
                        TimeSpan
                            .FromMilliseconds(
                                80),

                    AutoReverse =
                        true
                };

            CardBorder.BeginAnimation(
                OpacityProperty,
                animation);
        }
    }
}