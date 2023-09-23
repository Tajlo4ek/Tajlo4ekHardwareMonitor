using System.Collections.Generic;
using Tajlo4ekHardwareMonitor.Controls;
using LibreHardwareMonitor.Hardware;
using System.Text;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Linq;
using System.Drawing;

namespace Tajlo4ekHardwareMonitor
{
    internal class ComputerViewManager : IDisposable
    {
        private class PlotBind
        {
            readonly PlotDrawer drawer;
            readonly ISensor sensor;
            private float prevValue;

            public PlotBind(PlotDrawer drawer, ISensor sensor)
            {
                this.drawer = drawer;
                this.sensor = sensor;
            }

            public PlotDrawer GetDrawer()
            {
                return drawer;
            }

            public void Update()
            {
                float value = sensor.Value == null ? 0 : (float)sensor.Value;
                if (value == 0)
                {
                    value = prevValue;
                }
                else
                {
                    prevValue = value;
                }
                drawer.AddValue(value);
            }
        }

        private class ClockVoltage
        {
            public class Data
            {
                public ISensor voltage;
                public ISensor clock;
            }

            public List<Data> clockVoltages;
            public Label label = null;

            public ClockVoltage()
            {
                clockVoltages = new List<Data>();
            }

            public void Update()
            {
                if (label == null) { return; }

                string text = "";

                int num = 0;
                foreach (var data in clockVoltages)
                {
                    if (num != 0 && num % 2 == 0)
                    {
                        text += "\n";
                    }
                    else
                    {
                        text += "  ";
                    }

                    if (data.clock == null || data.voltage == null)
                    {
                        return;
                    }

                    text += string.Format(
                        "CPU#{0,-2} clock: {1,4}MHz, voltage: {2,1}V",
                        num + 1,
                        Math.Round(data.clock.Value ?? 0),
                        Math.Round(data.voltage.Value ?? 0, 1));

                    num++;
                }

                label.Text = text.Trim();
            }
        }

        public class UpdateVisitor : IVisitor
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

        IHardware cpu;
        readonly List<PlotBind> cpuLoadPlots;
        readonly List<PlotBind> cpuTempPlots;
        readonly ClockVoltage cpuClockVoltage;

        public ComputerViewManager()
        {
            cpuLoadPlots = new List<PlotBind>();
            cpuTempPlots = new List<PlotBind>();
            cpuClockVoltage = new ClockVoltage();

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

            computer.Open();
            FindSensors();
        }

        private void FindSensors()
        {
            computer.Accept(new UpdateVisitor());

            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Network) { continue; }

                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    cpu = hardware;
                    cpuClockVoltage.clockVoltages.Clear();
                    cpuLoadPlots.Clear();
                    cpuTempPlots.Clear();
                }

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        BindSensor(sensor);
                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    BindSensor(sensor);
                }
            }
        }

        private void BindSensor(ISensor sensor)
        {
            if (new Regex("CPU Core #\\d+ Thread #\\d+").Matches(sensor.Name).Count > 0 && sensor.SensorType == SensorType.Load)
            {
                var numbers = new Regex("\\d+").Matches(sensor.Name);
                if (numbers.Count != 2) { return; }
                string name = "Core #" + (int.Parse(numbers[0].ToString()) * 2 + int.Parse(numbers[1].ToString()) - 2);

                var plot = new PlotDrawer();
                plot.SetName(name, PlotDrawer.ValueType.Load);
                cpuLoadPlots.Add(new PlotBind(plot, sensor));
            }
            else if (new Regex("CPU Core #\\d+$").Matches(sensor.Name).Count > 0)
            {
                string name = sensor.Name.Replace("CPU ", "");

                switch (sensor.SensorType)
                {
                    case SensorType.Temperature:
                        {
                            var plot = new PlotDrawer();
                            plot.SetName(name, PlotDrawer.ValueType.Temp);
                            cpuTempPlots.Add(new PlotBind(plot, sensor));
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

        public GroupBox GenerateViewCpu()
        {
            GroupBox cpuGroup = new GroupBox
            {
                Text = cpu.Name,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            TableLayoutPanel tlpCpu = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new System.Drawing.Point(6, 19),
                Size = new System.Drawing.Size(10, 10),
                ColumnCount = 3
            };

            cpuClockVoltage.label = new Label() { Font = new Font("Consolas", 8.25f) };

            ControlPlaceManager.Connect(
                tlpCpu,
                cpuClockVoltage.label,
                ControlPlaceManager.PositionH.Left,
                ControlPlaceManager.PositionV.Bottom,
                5);

            cpuGroup.Controls.Add(tlpCpu);
            cpuGroup.Controls.Add(cpuClockVoltage.label);

            int totalPlot = cpuLoadPlots.Count + cpuTempPlots.Count;
            tlpCpu.RowCount = totalPlot / tlpCpu.ColumnCount;
            if (tlpCpu.ColumnCount * tlpCpu.RowCount < totalPlot)
            {
                tlpCpu.RowCount++;
            }

            tlpCpu.RowStyles.Clear();
            for (int row = 0; row < tlpCpu.RowCount; row++)
            {
                tlpCpu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int column = 0; column < tlpCpu.ColumnCount; column++)
                {
                    tlpCpu.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                }
            }

            for (int i = 0; i < cpuTempPlots.Count; i++)
            {
                tlpCpu.Controls.Add(cpuLoadPlots[i * 2 + 0].GetDrawer());
                tlpCpu.Controls.Add(cpuLoadPlots[i * 2 + 1].GetDrawer());
                tlpCpu.Controls.Add(cpuTempPlots[i].GetDrawer());
            }

            foreach (PlotDrawer plot in tlpCpu.Controls)
            {
                ValidatePlots(plot);
            }

            cpuClockVoltage.label.Size = new System.Drawing.Size(tlpCpu.Size.Width, 40);

            return cpuGroup;
        }

        private void ValidatePlots(PlotDrawer plotDrawer)
        {
            plotDrawer.Size = new System.Drawing.Size(145, 80);
            plotDrawer.Size = plotDrawer.ValidateSize();
        }


        public string GetFullDataString()
        {
            computer.Accept(new UpdateVisitor());

            StringBuilder stringBuilder = new StringBuilder();

            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Network) { continue; }

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
            UpdatePlots(cpuTempPlots);
            UpdatePlots(cpuLoadPlots);
            cpuClockVoltage.Update();
        }

        private void UpdatePlots(List<PlotBind> list)
        {
            foreach (var data in list)
            {
                data.Update();
            }
        }
    }
}
