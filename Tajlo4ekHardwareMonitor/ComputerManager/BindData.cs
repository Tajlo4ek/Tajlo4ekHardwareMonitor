using System;
using System.Collections.Generic;
using Tajlo4ekHardwareMonitor.Controls;
using LibreHardwareMonitor.Hardware;
using System.Drawing;
using System.Windows.Forms;

namespace Tajlo4ekHardwareMonitor.DataBind
{

    interface IUpdatable
    {
        void Update();
    }

    class PlotBind : IUpdatable
    {
        readonly PlotDrawer drawer;
        readonly ISensor sensor;
        private float prevValue = 0;

        public PlotBind()
        {
            this.drawer = new PlotDrawer();
            this.drawer.SetName("Nan", PlotDrawer.ValueType.Load);
            this.sensor = null;
        }

        public PlotBind(string plotName, PlotDrawer.ValueType type, ISensor sensor) : this()
        {
            this.drawer.SetName(plotName, type);
            this.sensor = sensor;
        }

        public PlotDrawer GetDrawer()
        {
            return drawer;
        }

        public void Update()
        {

            float value = sensor == null ? (0) : (sensor.Value == null ? 0 : (float)sensor.Value);
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

    class LabelBind : IUpdatable
    {
        public static readonly Font LabelFont = new Font("Consolas", 9f);

        protected string unit = "";
        protected string name = "";
        public Label label;
        public ISensor Sensor { get; protected set; }

        public LabelBind()
        {
            this.label = new Label() { Font = LabelFont, AutoSize = true };
            this.Sensor = null;
        }

        public virtual void SetSensor(ISensor sensor, string name, string unit)
        {
            this.unit = unit;
            this.name = name;
            this.Sensor = sensor;
        }

        public virtual void Update()
        {
            this.label.Text = Sensor == null ? "" : string.Format("{0,10}: {1,4} {2}", name, Math.Round(Sensor.Value ?? 0, 1), unit);
        }
    }

    class LabelBindStorage : LabelBind
    {
        public override void SetSensor(ISensor sensor, string name, string unit)
        {
            this.unit = "Mb/s";
            this.name = name;
            this.Sensor = sensor;
        }

        public override void Update()
        {
            var value = Math.Round((Sensor.Value ?? 0) / 1024);

            this.label.Text = Sensor == null ? "" : string.Format("{0,10}: {1,4} {2}", name, Math.Round((Sensor.Value ?? 0) / (1024 * 1024)), unit);
        }
    }

    class LabelBindTotalBytesWrite : LabelBind
    {
        public LabelBindTotalBytesWrite(ISensor sensor, string name)
        {
            SetSensor(sensor, name, "Gb");
        }

        public override void SetSensor(ISensor sensor, string name, string unit)
        {
            this.name = name;
            this.Sensor = sensor;
        }

        public override void Update()
        {
            var value = (Sensor.Value ?? 0) / 1024.0;
            var unit = value < 1 ? "Gb" : "Tb";

            if (value < 1)
            {
                value = Math.Round(value * 1024);
            }
            else if (value < 10)
            {
                value = Math.Round(value, 2);
            }
            else if (value < 100)
            {
                value = Math.Round(value, 1);
            }
            else
            {
                value = Math.Round(value * 1024);
            }

            this.label.Text = Sensor == null ? "" :
            string.Format(
                    "{0,10}: {1,4} {2}",
                    name,
                    value,
                    unit);
        }
    }

    class LabelBindTotalReadWrite : LabelBind
    {
        public LabelBindTotalReadWrite(ISensor sensor, string name)
        {
            SetSensor(sensor, name, "Gb");
        }

        public new void SetSensor(ISensor sensor, string name, string unit)
        {
            this.unit = "Gb";
            this.name = name;
            this.Sensor = sensor;
        }

        public override void Update()
        {
            var value = (Sensor.Value ?? 0) * Math.Pow(1000, 4) / Math.Pow(1024, 5);

            var unit = value < 1 ? "Gb" : "Tb";
            if (value < 1)
            {
                value = Math.Round(value * 1024);
            }
            else if (value < 10)
            {
                value = Math.Round(value, 2);
            }
            else if (value < 100)
            {
                value = Math.Round(value, 1);
            }
            else
            {
                value = Math.Round(value * 1024);
            }

            this.label.Text = Sensor == null ? "" :
            string.Format(
                    "{0,10}: {1,4} {2}",
                    name,
                    value,
                    unit);
        }
    }

    class GpuLabels : IUpdatable
    {
        public LabelBind fanRpm = new LabelBind();
        public LabelBind memoryTotal = new LabelBind();
        public LabelBind gpuClock = new LabelBind();
        public LabelBind power = new LabelBind();
        public LabelBind hotSpot = new LabelBind();

        public void Update()
        {
            fanRpm.Update();
            memoryTotal.Update();
            gpuClock.Update();
            hotSpot.Update();
            power.Update();
        }
    }

    class RamLabels : IUpdatable
    {
        public LabelBind free = new LabelBind();
        public LabelBind used = new LabelBind();

        public void Update()
        {
            free.Update();
            used.Update();
        }
    }

    class StorageLabels : IUpdatable
    {
        public LabelBindStorage readRate = new LabelBindStorage();
        public LabelBindStorage writeRate = new LabelBindStorage();
        public LabelBind dataReaded = new LabelBind();
        public LabelBind dataWrited = new LabelBind();
        public LabelBind usedSpace = new LabelBind();

        public void Update()
        {
            readRate.Update();
            writeRate.Update();
            dataReaded.Update();
            dataWrited.Update();
            usedSpace.Update();
        }
    }

    class ClockVoltage : IUpdatable
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
            //TODO: auto size Remove
            label = new Label() { Font = new Font("Consolas", 8.25f), AutoSize = true };
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

                var value = Math.Round(data.clock.Value ?? 0);
                if (value > 10000)
                {
                    value = 0;
                }

                text += string.Format(
                    "CPU#{0,-2} clock: {1,4}MHz, voltage: {2,1}V",
                    num + 1,
                    value,
                    Math.Round(data.voltage.Value ?? 0, 1));

                num++;
            }

            label.Text = text.Trim();

            if (label.MinimumSize.Width == 0)
            {
                label.MinimumSize = label.Size;
                label.MaximumSize = label.Size;
            }
        }
    }

}
