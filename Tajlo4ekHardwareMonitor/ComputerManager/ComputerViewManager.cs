using System.Collections.Generic;
using Tajlo4ekHardwareMonitor.Controls;
using LibreHardwareMonitor.Hardware;
using Tajlo4ekHardwareMonitor.DataBind;
using System.Text;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;

namespace Tajlo4ekHardwareMonitor
{
    internal class ComputerViewManager : IDisposable
    {

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }

            public void VisitSensor(ISensor sensor)
            {
            }

            public void VisitParameter(IParameter parameter)
            {
            }
        }

        readonly Computer computer;
        readonly List<IUpdatable> updatables;

        IHardware cpu;
        IHardware gpu;
        IHardware ram;
        List<IHardware> disks;

        public ComputerViewManager()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };

            updatables = new List<IUpdatable>();

            computer.Open();
            FindHardware();
        }

        private void FindCpuSensors(
            IHardware hardware,
            out List<PlotBind> cpuLoadPlots,
            out List<PlotBind> cpuTempPlots,
            out ClockVoltage cpuClockVoltage)
        {
            cpuLoadPlots = new List<PlotBind>();
            cpuTempPlots = new List<PlotBind>();
            cpuClockVoltage = new ClockVoltage();

            foreach (var sensor in GetAllSensors(hardware))
            {
                if (new Regex("CPU Core #\\d+ Thread #\\d+").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    var numbers = new Regex("\\d+").Matches(sensor.Name);
                    if (numbers.Count != 2) { return; }
                    string name = "Core #" + (int.Parse(numbers[0].ToString()) * 2 + int.Parse(numbers[1].ToString()) - 2);

                    cpuLoadPlots.Add(new PlotBind(name, PlotDrawer.ValueType.Load, sensor));
                }
                else if (new Regex("CPU Core #\\d+$").Matches(sensor.Name).Count > 0)
                {
                    string name = sensor.Name.Replace("CPU ", "");

                    switch (sensor.SensorType)
                    {
                        case SensorType.Temperature:
                            {
                                cpuTempPlots.Add(new PlotBind(name, PlotDrawer.ValueType.Temp, sensor));
                            }
                            break;

                        case SensorType.Clock:
                        case SensorType.Voltage:
                            {
                                var number = int.Parse(new Regex("\\d+").Match(sensor.Name).ToString());

                                if (cpuClockVoltage.clockVoltages.Count < number)
                                {
                                    cpuClockVoltage.clockVoltages.Add(new ClockVoltage.Data());
                                }

                                number--;

                                if (sensor.SensorType == SensorType.Clock)
                                {
                                    cpuClockVoltage.clockVoltages[number].clock = sensor;
                                }
                                else
                                {
                                    cpuClockVoltage.clockVoltages[number].voltage = sensor;
                                }
                            }
                            break;
                    }

                }

            }
        }

        private void FindGpuSensors(
            IHardware hardware,
            out PlotBind gpuTemp,
            out PlotBind gpuLoad,
            out PlotBind gpuMemory,
            out GpuLabels gpuLabels)
        {
            gpuTemp = new PlotBind();
            gpuLoad = new PlotBind();
            gpuMemory = new PlotBind();
            gpuLabels = new GpuLabels();

            foreach (var sensor in GetAllSensors(hardware))
            {
                string name = sensor.Name;

                if (new Regex("GPU Core").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Temperature)
                {
                    gpuTemp = new PlotBind("GPU temp", PlotDrawer.ValueType.Temp, sensor);
                }
                else if (new Regex("GPU Hot Spot").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Temperature)
                {
                    gpuLabels.hotSpot.SetSensor(sensor, "Hot spot", "°C");
                }
                else if (new Regex("GPU Core").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    gpuLoad = new PlotBind("GPU load", PlotDrawer.ValueType.Load, sensor);
                }
                else if (new Regex("GPU Memory").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    gpuMemory = new PlotBind("GPU memory", PlotDrawer.ValueType.Load, sensor);
                }
                else if (new Regex("GPU Core").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Clock)
                {
                    gpuLabels.gpuClock.SetSensor(sensor, "GPU core", "MHz");
                }
                else if (new Regex("GPU Memory Total").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.SmallData)
                {
                    gpuLabels.memoryTotal.SetSensor(sensor, "Memory", "Mb");
                }
                else if (new Regex("GPU Fan").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Fan)
                {
                    gpuLabels.fanRpm.SetSensor(sensor, "Fan speed", "Rpm");
                }
                else if (new Regex("GPU Board Power").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    gpuLabels.power.SetSensor(sensor, "Power", "W");
                }
            }
        }

        private void FindRamSensors(IHardware hardware, out PlotBind ramPlotBind, out RamLabels ramLabels)
        {
            ramPlotBind = new PlotBind();
            ramLabels = new RamLabels();

            foreach (var sensor in GetAllSensors(hardware))
            {
                if (new Regex("^Memory Used$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Data)
                {
                    ramLabels.used.SetSensor(sensor, "Used", "Gb");
                }
                else if (new Regex("^Memory Available$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Data)
                {
                    ramLabels.free.SetSensor(sensor, "Free", "Gb");
                }
                else if (new Regex("^Memory$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    ramPlotBind = new PlotBind("Ram use", PlotDrawer.ValueType.Used, sensor);
                }
            }
        }

        private void FindStorageSensors(IHardware hardware, out PlotBind stageUsage, out PlotBind temp, out StorageLabels storageLabels)
        {
            stageUsage = new PlotBind();
            temp = new PlotBind();
            storageLabels = new StorageLabels();

            foreach (var sensor in GetAllSensors(hardware))
            {
                if (new Regex("^Temperature$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Temperature)
                {
                    temp = new PlotBind("Temp", PlotDrawer.ValueType.Temp, sensor);
                }
                else if (new Regex("^Total Activity$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    stageUsage = new PlotBind("Usage", PlotDrawer.ValueType.Used, sensor);
                }
                else if (new Regex("^Read Rate$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Throughput)
                {
                    storageLabels.readRate.SetSensor(sensor, "Read rate", "Mb/s");
                }
                else if (new Regex("^Write Rate$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Throughput)
                {
                    storageLabels.writeRate.SetSensor(sensor, "Write rate", "Mb/s");
                }
                else if (new Regex("^Used Space$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
                {
                    storageLabels.usedSpace.SetSensor(sensor, "Used space", "%");
                }
                else if (new Regex("^Data Read$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Data)
                {
                    storageLabels.dataReaded = new LabelBindTotalReadWrite(sensor, "Tot readed");
                }
                else if (new Regex("^Data Written$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Data)
                {
                    storageLabels.dataWrited = new LabelBindTotalReadWrite(sensor, "Tot writed");
                }
                else if (new Regex("^Total Bytes Written$").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Data)
                {
                    storageLabels.dataWrited = new LabelBindTotalBytesWrite(sensor, "Tot writed");
                }
            }
        }

        private void FindHardware()
        {
            computer.Accept(new UpdateVisitor());
            disks = new List<IHardware>();

            foreach (IHardware hardware in computer.Hardware)
            {
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu: { cpu = hardware; } break;
                    case HardwareType.GpuNvidia: { gpu = hardware; } break;
                    case HardwareType.Memory: { ram = hardware; } break;
                    case HardwareType.Storage: { disks.Add(hardware); } break;
                    default: break;
                }
            }
        }

        private List<ISensor> GetAllSensors(IHardware hardware)
        {
            List<ISensor> sensors = new List<ISensor>();

            foreach (IHardware subhardware in hardware.SubHardware)
            {
                foreach (ISensor sensor in subhardware.Sensors)
                {
                    sensors.Add(sensor);
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                sensors.Add(sensor);
            }

            return sensors;
        }

        public Control GenerateViewCpu()
        {
            GroupBox cpuGroup = new GroupBox
            {
                Text = cpu.Name,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            FindCpuSensors(cpu, out List<PlotBind> cpuLoadPlots, out List<PlotBind> cpuTempPlots, out ClockVoltage cpuClockVoltage);

            cpuGroup.Controls.Add(cpuClockVoltage.label);
            updatables.Add(cpuClockVoltage);

            Control prevControl = null;
            for (int i = 0; i < cpuTempPlots.Count; i++)
            {
                var load1Plot = cpuLoadPlots[i * 2 + 0];
                updatables.Add(load1Plot);

                var load2Plot = cpuLoadPlots[i * 2 + 1];
                updatables.Add(load2Plot);

                var tempPlot = cpuTempPlots[i];
                updatables.Add(tempPlot);

                var load1Drawer = load1Plot.GetDrawer();
                var load2Drawer = load2Plot.GetDrawer();
                var tempDrawer = tempPlot.GetDrawer();

                cpuGroup.Controls.Add(load1Drawer);
                cpuGroup.Controls.Add(load2Drawer);
                cpuGroup.Controls.Add(tempDrawer);

                ControlPlaceManager.Connect(
                    load1Drawer,
                    load2Drawer,
                    ControlPlaceManager.PositionH.Right,
                    ControlPlaceManager.PositionV.Top,
                    7);

                ControlPlaceManager.Connect(
                    load2Drawer,
                    tempDrawer,
                    ControlPlaceManager.PositionH.Right,
                    ControlPlaceManager.PositionV.Top,
                    7);

                if (prevControl != null)
                {
                    ControlPlaceManager.Connect(
                        prevControl,
                        load1Drawer,
                        ControlPlaceManager.PositionH.Left,
                        ControlPlaceManager.PositionV.Bottom,
                        7);
                }
                else
                {
                    load1Drawer.Location = new Point(6, 19);
                }

                prevControl = load1Drawer;
            }

            ControlPlaceManager.Connect(
                prevControl,
                cpuClockVoltage.label,
                ControlPlaceManager.PositionH.Left,
                ControlPlaceManager.PositionV.Bottom,
                5);

            ValidatePlots(cpuGroup);
            return cpuGroup;
        }

        public Control GenerateViewGpu()
        {
            GroupBox gpuGroup = new GroupBox
            {
                Text = gpu.Name,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };

            FindGpuSensors(gpu,
                out PlotBind gpuTemp,
                out PlotBind gpuLoad,
                out PlotBind gpuMemory,
                out GpuLabels gpuLabels);

            updatables.Add(gpuLabels);

            Control mainControl = gpuLoad.GetDrawer();
            gpuGroup.Controls.Add(mainControl);
            updatables.Add(gpuLoad);

            gpuGroup.Controls.Add(gpuMemory.GetDrawer());
            updatables.Add(gpuMemory);

            gpuGroup.Controls.Add(gpuTemp.GetDrawer());
            updatables.Add(gpuTemp);

            gpuGroup.Controls.Add(gpuLabels.gpuClock.label);
            gpuGroup.Controls.Add(gpuLabels.fanRpm.label);
            gpuGroup.Controls.Add(gpuLabels.power.label);
            gpuGroup.Controls.Add(gpuLabels.hotSpot.label);
            gpuGroup.Controls.Add(gpuLabels.memoryTotal.label);


            ControlPlaceManager.Connect(mainControl, gpuTemp.GetDrawer(), ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 7);
            ControlPlaceManager.Connect(mainControl, gpuMemory.GetDrawer(), ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 7);

            ControlPlaceManager.Connect(gpuTemp.GetDrawer(), gpuLabels.gpuClock.label, ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 7);
            ControlPlaceManager.Connect(gpuLabels.gpuClock.label, gpuLabels.memoryTotal.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
            ControlPlaceManager.Connect(gpuLabels.memoryTotal.label, gpuLabels.power.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
            ControlPlaceManager.Connect(gpuLabels.power.label, gpuLabels.hotSpot.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
            ControlPlaceManager.Connect(gpuLabels.hotSpot.label, gpuLabels.fanRpm.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);

            gpuLoad.GetDrawer().Location = new Point(6, 19);

            gpuGroup.Margin = new Padding(0, 0, 0, 0);
            gpuGroup.Padding = new Padding(0, 0, 3, 0);

            ValidatePlots(gpuGroup);
            return gpuGroup;
        }

        public Control GenerateViewRam()
        {
            GroupBox ramGroup = new GroupBox
            {
                Text = "RAM",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };

            FindRamSensors(ram, out PlotBind ramPlotData, out RamLabels ramLabels);
            updatables.Add(ramPlotData);
            updatables.Add(ramLabels);

            ramGroup.Controls.Add(ramPlotData.GetDrawer());
            ramGroup.Controls.Add(ramLabels.free.label);
            ramGroup.Controls.Add(ramLabels.used.label);

            ControlPlaceManager.Connect(ramPlotData.GetDrawer(), ramLabels.free.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 7);
            ControlPlaceManager.Connect(ramLabels.free.label, ramLabels.used.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);

            ramPlotData.GetDrawer().Location = new Point(6, 19);

            ValidatePlots(ramGroup);
            return ramGroup;
        }

        public List<Control> GenerateViewDisks()
        {
            List<Control> controls = new List<Control>();

            foreach (var storage in disks)
            {
                GroupBox storageGroup = new GroupBox
                {
                    Text = storage.Name,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                };

                FindStorageSensors(storage, out PlotBind stageUsage, out PlotBind temp, out StorageLabels storageLabels);

                updatables.Add(stageUsage);
                updatables.Add(temp);
                updatables.Add(storageLabels);

                storageGroup.Controls.Add(stageUsage.GetDrawer());
                storageGroup.Controls.Add(temp.GetDrawer());
                storageGroup.Controls.Add(storageLabels.dataWrited.label);
                storageGroup.Controls.Add(storageLabels.readRate.label);
                storageGroup.Controls.Add(storageLabels.writeRate.label);
                storageGroup.Controls.Add(storageLabels.dataReaded.label);
                storageGroup.Controls.Add(storageLabels.usedSpace.label);

                ControlPlaceManager.Connect(temp.GetDrawer(), stageUsage.GetDrawer(), ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 7);
                ControlPlaceManager.Connect(stageUsage.GetDrawer(), storageLabels.writeRate.label, ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 7);
                ControlPlaceManager.Connect(storageLabels.writeRate.label, storageLabels.readRate.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
                ControlPlaceManager.Connect(storageLabels.readRate.label, storageLabels.usedSpace.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
                ControlPlaceManager.Connect(storageLabels.usedSpace.label, storageLabels.dataWrited.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);
                ControlPlaceManager.Connect(storageLabels.dataWrited.label, storageLabels.dataReaded.label, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 0);

                temp.GetDrawer().Location = new Point(6, 19);

                ValidatePlots(storageGroup);

                controls.Add(storageGroup);
            }

            return controls;
        }

        private void ValidatePlots(Control control)
        {
            foreach (Control plot in control.Controls)
            {
                if (plot is PlotDrawer drawer)
                {
                    drawer.Size = new System.Drawing.Size(145, 80);
                    drawer.Size = drawer.ValidateSize();
                }
            }

        }

        public string GetFullDataString()
        {
            computer.Accept(new UpdateVisitor());

            StringBuilder stringBuilder = new StringBuilder();

            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Network
                    || hardware.HardwareType == HardwareType.Cpu
                    || hardware.HardwareType == HardwareType.GpuNvidia
                    || hardware.HardwareType == HardwareType.Memory
                    ) { continue; }

                stringBuilder.Append(string.Format("Hardware: {0}\n", hardware.Name));

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    stringBuilder.Append(string.Format("\tSubhardware: {0}\n", subhardware.Name));

                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        stringBuilder.Append(string.Format("\t\tSensor: {0}, value: {1}, type: {2}\n", sensor.Name, sensor.Value, sensor.SensorType));

                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    stringBuilder.Append(string.Format("\t\tSensor: {0}, value: {1}, type: {2}\n", sensor.Name, sensor.Value, sensor.SensorType));

                }
            }

            return stringBuilder.ToString();
        }

        public void Dispose()
        {
            computer.Close();
        }

        public void Update()
        {
            computer.Accept(new UpdateVisitor());

            foreach (var item in updatables)
            {
                item.Update();
            }
        }
    }
}
