namespace WinBoost.App.Models
{
    public sealed class CpuTemperatureInfo
    {
        public bool IsAvailable { get; init; }

        public float Celsius { get; init; }

        public string SensorName { get; init; } =
            string.Empty;
    }
}