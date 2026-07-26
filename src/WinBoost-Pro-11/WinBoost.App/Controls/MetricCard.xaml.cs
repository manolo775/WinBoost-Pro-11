using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace WinBoost.App.Controls
{
    /// <summary>
    /// Interaction logic for MetricCard.xaml
    /// </summary>
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
        new PropertyMetadata(0.0));

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
                new PropertyMetadata(PackIconKind.DesktopClassic));

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
        public static readonly DependencyProperty ShowProgressBarProperty =
    DependencyProperty.Register(
        nameof(ShowProgressBar),
        typeof(bool),
        typeof(MetricCard),
        new PropertyMetadata(true));

        public bool ShowProgressBar
        {
            get => (bool)GetValue(ShowProgressBarProperty);
            set => SetValue(ShowProgressBarProperty, value);
        }
    }
}
