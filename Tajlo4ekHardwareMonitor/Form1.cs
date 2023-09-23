
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Tajlo4ekHardwareMonitor.Controls;

namespace Tajlo4ekHardwareMonitor
{
    public partial class Form1 : Form
    {

        private readonly ComputerViewManager computerViewManager;
        private readonly ControlPlaceManager placeManager;

        public Form1()
        {
            InitializeComponent();

            placeManager = new ControlPlaceManager();

            computerViewManager = new ComputerViewManager();

            var cpuGroup = computerViewManager.GenerateViewCpu();
            cpuGroup.Location = new System.Drawing.Point(12, 5);
            this.Controls.Add(cpuGroup);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = computerViewManager.GetFullDataString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            computerViewManager?.Dispose();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            computerViewManager.Update();
        }


    }
}
