using System.Windows;
using System.Windows.Controls;

namespace WinBoost.App.Helpers
{
    public static class NativeMessageDialog
    {
        public static void Show(
            Window owner,
            string title,
            string message,
            string buttonText)
        {
            var dialog =
                new Window
                {
                    Title =
                        title,

                    Width =
                        520,

                    Height =
                        220,

                    MinWidth =
                        420,

                    MinHeight =
                        190,

                    ResizeMode =
                        ResizeMode.NoResize,

                    WindowStartupLocation =
                        WindowStartupLocation
                            .CenterOwner,

                    Owner =
                        owner,

                    ShowInTaskbar =
                        false,

                    Background =
                        SystemColors
                            .WindowBrush
                };

            var root =
                new Grid
                {
                    Margin =
                        new Thickness(
                            24)
                };

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                });

            var messageText =
                new TextBlock
                {
                    Text =
                        message,

                    TextWrapping =
                        TextWrapping.Wrap,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    FontSize =
                        15,

                    Margin =
                        new Thickness(
                            0,
                            0,
                            0,
                            20)
                };

            Grid.SetRow(
                messageText,
                0);

            var button =
                new Button
                {
                    Content =
                        buttonText,

                    Width =
                        120,

                    Height =
                        38,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    IsDefault =
                        true,

                    IsCancel =
                        true
                };

            button.Click +=
                (_, _) =>
                {
                    dialog.Close();
                };

            Grid.SetRow(
                button,
                1);

            root.Children.Add(
                messageText);

            root.Children.Add(
                button);

            dialog.Content =
                root;

            dialog.ShowDialog();
        }
    }
}