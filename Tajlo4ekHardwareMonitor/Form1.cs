
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tajlo4ekHardwareMonitor
{
    public partial class Form1 : Form
    {
        private readonly ComputerViewManager computerViewManager;

        readonly Control cpuControl;
        readonly Control gpuControl;
        readonly Control ramControl;
        readonly List<Control> storagesControl;
        private bool first = true;

        public Form1()
        {
            InitializeComponent();
            KeyPreview = true;

            computerViewManager = new ComputerViewManager();

            cpuControl = computerViewManager.GenerateViewCpu();
            gpuControl = computerViewManager.GenerateViewGpu();
            ramControl = computerViewManager.GenerateViewRam();
            storagesControl = computerViewManager.GenerateViewDisks();

            ControlPlaceManager.Connect(cpuControl, gpuControl, ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 5);
            ControlPlaceManager.Connect(gpuControl, ramControl, ControlPlaceManager.PositionH.Right, ControlPlaceManager.PositionV.Top, 5);

            this.Controls.Add(cpuControl);
            this.Controls.Add(gpuControl);
            this.Controls.Add(ramControl);

            var prevControl = gpuControl;
            foreach (var control in storagesControl)
            {
                this.Controls.Add(control);
                ControlPlaceManager.Connect(prevControl, control, ControlPlaceManager.PositionH.Left, ControlPlaceManager.PositionV.Bottom, 5);
                prevControl = control;
            }

            SetDarkTheme(this);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            computerViewManager?.Dispose();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            computerViewManager.Update();
            if (first)
            {
                cpuControl.Location = new System.Drawing.Point(12, 5);

                gpuControl.MinimumSize = new System.Drawing.Size(
                    gpuControl.Width,
                    cpuControl.Height - (storagesControl[storagesControl.Count - 1].Bottom - storagesControl[0].Top) - 5);

                ramControl.MinimumSize = new System.Drawing.Size(ramControl.Size.Width, gpuControl.Size.Height);
                foreach (var control in storagesControl)
                {
                    control.MinimumSize = new System.Drawing.Size(ramControl.Right - gpuControl.Left, control.Size.Height);
                }


                Form1_Resize(null, EventArgs.Empty);
                first = false;
            }
        }

        private void SetDarkTheme(Control control)
        {
            var darkBackground = System.Drawing.Color.FromArgb(30, 30, 30);
            var foreColor = System.Drawing.Color.FromArgb(0, 200, 0);

            control.BackColor = darkBackground;
            control.ForeColor = foreColor;

            foreach (Control subControl in control.Controls)
            {
                subControl.BackColor = darkBackground;

                if (subControl is GroupBox)
                {
                    SetDarkTheme(subControl);
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            var totWidth = ramControl.Right - cpuControl.Left;
            var totHeight = cpuControl.Height;

            var posX = (ClientRectangle.Width - totWidth) / 2;
            var posY = (ClientRectangle.Height - totHeight) / 2;
            cpuControl.Location = new System.Drawing.Point(posX, posY);

            if (this.WindowState == FormWindowState.Maximized)
            {
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

    }
}
