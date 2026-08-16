using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace WinBoost.App.Helpers
{
    public static class NativeConfirmationDialog
    {
        private const int WhCbt = 5;
        private const int HcbtActivate = 5;

        private const int IdOk = 1;
        private const int IdYes = 6;
        private const int IdNo = 7;

        [ThreadStatic]
        private static string? _okText;

        [ThreadStatic]
        private static string? _yesText;

        [ThreadStatic]
        private static string? _noText;

        [ThreadStatic]
        private static HookProcedure? _hookProcedure;

        [ThreadStatic]
        private static IntPtr _hookHandle;

        public static bool Ask(
            Window? owner,
            string title,
            string message,
            string yesText,
            string noText)
        {
            _okText = null;
            _yesText = yesText;
            _noText = noText;

            InstallHook();

            try
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        owner,
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                return result == MessageBoxResult.Yes;
            }
            finally
            {
                RemoveHook();
            }
        }

        public static void ShowAcknowledgement(
            Window? owner,
            string title,
            string message,
            string buttonText)
        {
            _okText = buttonText;
            _yesText = null;
            _noText = null;

            InstallHook();

            try
            {
                MessageBox.Show(
                    owner,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                RemoveHook();
            }
        }

        private static void InstallHook()
        {
            _hookProcedure =
                ConfirmationDialogHook;

            _hookHandle =
                SetWindowsHookEx(
                    WhCbt,
                    _hookProcedure,
                    IntPtr.Zero,
                    GetCurrentThreadId());
        }

        private static void RemoveHook()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(
                    _hookHandle);
            }

            _hookHandle = IntPtr.Zero;
            _hookProcedure = null;
            _okText = null;
            _yesText = null;
            _noText = null;
        }

        private static IntPtr ConfirmationDialogHook(
            int code,
            IntPtr windowHandle,
            IntPtr lParam)
        {
            if (code == HcbtActivate)
            {
                if (!string.IsNullOrWhiteSpace(_okText))
                {
                    SetDlgItemText(
                        windowHandle,
                        IdOk,
                        _okText);
                }

                if (!string.IsNullOrWhiteSpace(_yesText))
                {
                    SetDlgItemText(
                        windowHandle,
                        IdYes,
                        _yesText);
                }

                if (!string.IsNullOrWhiteSpace(_noText))
                {
                    SetDlgItemText(
                        windowHandle,
                        IdNo,
                        _noText);
                }
            }

            return CallNextHookEx(
                _hookHandle,
                code,
                windowHandle,
                lParam);
        }

        private delegate IntPtr HookProcedure(
            int code,
            IntPtr windowHandle,
            IntPtr lParam);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int hookId,
            HookProcedure hookProcedure,
            IntPtr moduleHandle,
            uint threadId);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(
            IntPtr hookHandle);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hookHandle,
            int code,
            IntPtr windowHandle,
            IntPtr lParam);

        [DllImport(
            "user32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern bool SetDlgItemText(
            IntPtr dialogHandle,
            int itemId,
            string text);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}