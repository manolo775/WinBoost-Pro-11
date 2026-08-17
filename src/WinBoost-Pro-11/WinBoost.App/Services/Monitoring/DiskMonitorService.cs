using System;
using System.IO;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class DiskMonitorService
    {
        public float GetDiskUsage()
        {
            string systemDrive =
                Path.GetPathRoot(
                    Environment.SystemDirectory)
                ?? throw new InvalidOperationException(
                    "System drive could not be determined.");

            var drive =
                new DriveInfo(
                    systemDrive);

            if (!drive.IsReady)
            {
                throw new IOException(
                    "System drive is not ready.");
            }

            if (drive.TotalSize <= 0)
            {
                throw new IOException(
                    "System drive size is unavailable.");
            }

            long usedSpace =
                drive.TotalSize -
                drive.AvailableFreeSpace;

            double usage =
                (double)usedSpace /
                drive.TotalSize *
                100.0;

            return (float)Math.Clamp(
                usage,
                0.0,
                100.0);
        }
    }
}