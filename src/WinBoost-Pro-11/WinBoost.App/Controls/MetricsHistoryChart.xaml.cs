using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WinBoost.App.Models;

namespace WinBoost.App.Controls
{
    public partial class MetricsHistoryChart : UserControl
    {
        public static readonly DependencyProperty
            ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IReadOnlyList<SystemMetricsHistoryPoint>),
                typeof(MetricsHistoryChart),
                new PropertyMetadata(
                    null,
                    OnItemsSourceChanged));

        public MetricsHistoryChart()
        {
            InitializeComponent();

            Loaded += MetricsHistoryChart_Loaded;
            SizeChanged += MetricsHistoryChart_SizeChanged;
        }

        public IReadOnlyList<SystemMetricsHistoryPoint>?
            ItemsSource
        {
            get =>
                (IReadOnlyList<SystemMetricsHistoryPoint>?)
                GetValue(ItemsSourceProperty);

            set => SetValue(
                ItemsSourceProperty,
                value);
        }

        private static void OnItemsSourceChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is MetricsHistoryChart chart)
            {
                chart.DrawChart();
            }
        }

        private void MetricsHistoryChart_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            DrawChart();
        }

        private void MetricsHistoryChart_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DrawChart();
        }

        private void DrawChart()
        {
            if (!IsLoaded ||
                ChartCanvas.ActualWidth <= 0 ||
                ChartCanvas.ActualHeight <= 0)
            {
                return;
            }

            ChartCanvas.Children.Clear();

            DrawGridLines();

            if (ItemsSource == null ||
                ItemsSource.Count == 0)
            {
                return;
            }

            DrawMetricLine(
                point => point.CpuUsage,
                Color.FromRgb(0, 200, 83));

            DrawMetricLine(
                point => point.RamUsage,
                Color.FromRgb(41, 182, 246));

            DrawMetricLine(
                point => point.DiskUsage,
                Color.FromRgb(255, 215, 64));
        }

        private void DrawGridLines()
        {
            double width =
                ChartCanvas.ActualWidth;

            double height =
                ChartCanvas.ActualHeight;

            for (int index = 1;
                 index < 4;
                 index++)
            {
                double y =
                    height * index / 4;

                var line = new Line
                {
                    X1 = 0,
                    X2 = width,
                    Y1 = y,
                    Y2 = y,
                    Stroke =
                        new SolidColorBrush(
                            Color.FromRgb(
                                67,
                                69,
                                74)),
                    StrokeThickness = 1
                };

                ChartCanvas.Children.Add(line);
            }
        }

        private void DrawMetricLine(
            Func<SystemMetricsHistoryPoint, double>
                valueSelector,
            Color color)
        {
            if (ItemsSource == null ||
                ItemsSource.Count == 0)
            {
                return;
            }

            double width =
                ChartCanvas.ActualWidth;

            double height =
                ChartCanvas.ActualHeight;

            var polyline = new Polyline
            {
                Stroke =
                    new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineJoin =
                    PenLineJoin.Round,
                StrokeStartLineCap =
                    PenLineCap.Round,
                StrokeEndLineCap =
                    PenLineCap.Round
            };

            for (int index = 0;
                 index < ItemsSource.Count;
                 index++)
            {
                double value =
                    Math.Clamp(
                        valueSelector(
                            ItemsSource[index]),
                        0,
                        100);

                double x =
                    ItemsSource.Count == 1
                        ? width / 2
                        : width * index /
                          (ItemsSource.Count - 1);

                double y =
                    height -
                    (value / 100 * height);

                polyline.Points.Add(
                    new Point(x, y));
            }

            ChartCanvas.Children.Add(polyline);
        }
    }
}