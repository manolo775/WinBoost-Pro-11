using System;
using System.IO;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class DiskMonitorService
    {
        public float GetDiskUsage()
        {
            string systemDrive =
                Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";

            var drive = new DriveInfo(systemDrive);

            if (!drive.IsReady || drive.TotalSize == 0)
            {
                return 0;
            }

            long usedSpace =
                drive.TotalSize - drive.AvailableFreeSpace;

            double usage =
                (double)usedSpace / drive.TotalSize * 100;

            return (float)usage;
        }
    }
}