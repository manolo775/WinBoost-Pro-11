using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public class RecycleBinCleanerService
    {
        private const uint SherbNoConfirmation = 0x00000001;
        private const uint SherbNoProgressUi = 0x00000002;
        private const uint SherbNoSound = 0x00000004;

        public Task<RecycleBinStatus> GetRecycleBinStatusAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    RecycleBinInfo recycleBinInfo =
                        GetRecycleBinInfo();

                    return new RecycleBinStatus
                    {
                        IsSuccessful = true,
                        ItemCount = recycleBinInfo.ItemCount,
                        TotalSize = recycleBinInfo.TotalSize,
                        Message =
                            recycleBinInfo.ItemCount == 0
                                ? "Coșul de reciclare este gol."
                                : "Informațiile despre Coșul de reciclare au fost citite."
                    };
                }
                catch (Exception ex)
                {
                    return new RecycleBinStatus
                    {
                        IsSuccessful = false,
                        Message =
                            "Informațiile despre Coșul de reciclare " +
                            "nu au putut fi citite: " +
                            ex.Message
                    };
                }
            });
        }

        public Task<OptimizationResult> EmptyRecycleBinAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    RecycleBinInfo recycleBinInfo =
                        GetRecycleBinInfo();

                    if (recycleBinInfo.ItemCount == 0)
                    {
                        return new OptimizationResult
                        {

                            OperationId = "recycle-bin",
                            OperationName = "Recycle Bin",
                            RequiresAdministrator = false,

                            IsSuccessful = true,
                            DeletedFilesCount = 0,
                            RecoveredBytes = 0,
                            Message =
                                "Coșul de reciclare este deja gol."
                        };
                    }

                    uint flags =
                        SherbNoConfirmation |
                        SherbNoProgressUi |
                        SherbNoSound;

                    int result =
                        SHEmptyRecycleBin(
                            IntPtr.Zero,
                            null,
                            flags);

                    if (result != 0)
                    {
                        Marshal.ThrowExceptionForHR(result);
                    }

                    return new OptimizationResult
                    {
                        OperationId = "recycle-bin",
                        OperationName = "Recycle Bin",
                        RequiresAdministrator = false,


                        IsSuccessful = true,

                        DeletedFilesCount =
                            recycleBinInfo.ItemCount,

                        RecoveredBytes =
                            recycleBinInfo.TotalSize,

                        Message =
                            $"Coșul de reciclare a fost golit. " +
                            $"{recycleBinInfo.ItemCount} elemente " +
                            $"au fost eliminate."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {

                        OperationId = "recycle-bin",
                        OperationName = "Recycle Bin",
                        RequiresAdministrator = false,

                        IsSuccessful = false,

                        Message =
                            "Coșul de reciclare nu a putut fi golit: " +
                            ex.Message
                    };
                }
            });
        }

        private static RecycleBinInfo GetRecycleBinInfo()
        {
            var queryInfo =
                new SHQueryRBInfo
                {
                    StructureSize =
                        Marshal.SizeOf<SHQueryRBInfo>()
                };

            int result =
                SHQueryRecycleBin(
                    null,
                    ref queryInfo);

            if (result != 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return new RecycleBinInfo
            {
                TotalSize = queryInfo.TotalSize,

                ItemCount =
                    queryInfo.ItemCount > long.MaxValue
                        ? long.MaxValue
                        : (long)queryInfo.ItemCount
            };
        }

        private sealed class RecycleBinInfo
        {
            public long TotalSize { get; init; }

            public long ItemCount { get; init; }
        }

        [StructLayout(
            LayoutKind.Sequential,
            Pack = 8)]
        private struct SHQueryRBInfo
        {
            public int StructureSize;

            public long TotalSize;

            public ulong ItemCount;
        }

        [DllImport(
            "shell32.dll",
            CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(
            IntPtr windowHandle,
            string? rootPath,
            uint flags);

        [DllImport(
            "shell32.dll",
            CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(
            string? rootPath,
            ref SHQueryRBInfo queryInfo);
    }

    public class RecycleBinStatus
    {
        public bool IsSuccessful { get; set; }

        public long ItemCount { get; set; }

        public long TotalSize { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public string TotalSizeText =>
            FormatBytes(TotalSize);

        private static string FormatBytes(long bytes)
        {
            string[] units =
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:F2} {units[unitIndex]}";
        }
    }
}