using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class CpuTemperatureMonitorService
        : IDisposable
    {
        private readonly Computer _computer;

        public CpuTemperatureMonitorService()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true
            };

            _computer.Open();
        }

        public CpuTemperatureInfo GetCpuTemperature()
        {
            try
            {
                _computer.Accept(
                    new UpdateVisitor());

                List<ISensor> sensors =
                    GetCpuTemperatureSensors()
                    .Where(sensor =>
                        sensor.Value.HasValue)
                    .ToList();

                if (sensors.Count == 0)
                {
                    return new CpuTemperatureInfo
                    {
                        IsAvailable = false
                    };
                }

                ISensor selectedSensor =
                    sensors.FirstOrDefault(sensor =>
                        sensor.Name.Contains(
                            "Package",
                            StringComparison.OrdinalIgnoreCase))
                    ?? sensors.FirstOrDefault(sensor =>
                        sensor.Name.Contains(
                            "Core Average",
                            StringComparison.OrdinalIgnoreCase))
                    ?? sensors.FirstOrDefault(sensor =>
                        sensor.Name.Contains(
                            "Core Max",
                            StringComparison.OrdinalIgnoreCase))
                    ?? sensors[0];

                return new CpuTemperatureInfo
                {
                    IsAvailable = true,
                    Celsius =
                        selectedSensor.Value!.Value,
                    SensorName =
                        selectedSensor.Name
                };
            }
            catch
            {
                return new CpuTemperatureInfo
                {
                    IsAvailable = false
                };
            }
        }

        public void Dispose()
        {
            _computer.Close();
        }

        private IEnumerable<ISensor>
            GetCpuTemperatureSensors()
        {
            foreach (IHardware hardware
                     in _computer.Hardware)
            {
                foreach (IHardware item
                         in GetHardwareTree(hardware))
                {
                    if (item.HardwareType !=
                        HardwareType.Cpu)
                    {
                        continue;
                    }

                    foreach (ISensor sensor
                             in item.Sensors)
                    {
                        if (sensor.SensorType ==
                            SensorType.Temperature)
                        {
                            yield return sensor;
                        }
                    }
                }
            }
        }

        private static IEnumerable<IHardware>
            GetHardwareTree(IHardware hardware)
        {
            yield return hardware;

            foreach (IHardware subHardware
                     in hardware.SubHardware)
            {
                foreach (IHardware item
                         in GetHardwareTree(subHardware))
                {
                    yield return item;
                }
            }
        }

        private sealed class UpdateVisitor : IVisitor
        {
            public void VisitComputer(
                IComputer computer)
            {
                computer.Traverse(this);
            }

            public void VisitHardware(
                IHardware hardware)
            {
                hardware.Update();

                foreach (IHardware subHardware
                         in hardware.SubHardware)
                {
                    subHardware.Accept(this);
                }
            }

            public void VisitSensor(
                ISensor sensor)
            {
            }

            public void VisitParameter(
                IParameter parameter)
            {
            }
        }
    }
}