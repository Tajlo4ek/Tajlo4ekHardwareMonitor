using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Tajlo4ekHardwareMonitor.Controls
{
    public partial class PlotDrawer : UserControl
    {
        readonly List<float> values;
        private string name = "";
        private string valueName = "";
        private string valueUnit = "";

        readonly Pen borderPen = new Pen(Color.FromArgb(0, 100, 0), 1);
        readonly Pen cellPen = new Pen(Color.FromArgb(0, 100, 0), 1);
        readonly Pen dataPen = new Pen(Color.FromArgb(0, 200, 0), 1);
        readonly Brush textBrush = new SolidBrush(Color.FromArgb(0, 200, 0));

        float min = float.MaxValue;
        float max = 0;

        public enum ValueType
        {
            Load,
            Temp,
        }

        public PlotDrawer()
        {
            InitializeComponent();

            pbMain.Paint += PbMain_Paint;

            values = new List<float>();

            for (int i = 0; i < 60; i++)
            {
                values.Add(0);
            }
        }

        public Size ValidateSize()
        {
            var size = pbMain.Size;

            var linesVSize = (int)(borderPen.Width * 2 + cellPen.Width * 5);
            var linesHSize = (int)(borderPen.Width * 2 + cellPen.Width * 9);

            size.Width = (int)((pbMain.Width - linesVSize) / 6) * 6 + linesVSize;
            size.Height = (int)((pbMain.Height - linesHSize) / 10) * 10 + linesHSize;

            return size;
        }

        private void PbMain_Paint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;

            graphics.Clear(Color.Black);

            graphics.DrawRectangle(borderPen, 0, 0, pbMain.Width - borderPen.Width, pbMain.Height - borderPen.Width);

            var fieldHOffset = (int)borderPen.Width;
            var fieldVOffset = (int)borderPen.Width + 19;


            var fieldWidth = pbMain.Width - fieldHOffset - borderPen.Width;
            var fieldHeight = pbMain.Height - fieldVOffset - borderPen.Width;

            var oneCellHSpace = (int)((fieldWidth - (int)cellPen.Width * 5) / 6);
            var oneCellVSpace = (int)((fieldHeight - (int)cellPen.Width * 9) / 10);

            int posX = 0;
            for (int x = 0; x < 5; x++)
            {
                posX += oneCellHSpace + (int)cellPen.Width;
                graphics.DrawLine(cellPen, posX, fieldVOffset, posX, fieldHeight + fieldVOffset);
            }

            int posY = fieldVOffset;
            for (int y = 0; y < 10; y++)
            {
                graphics.DrawLine(cellPen, fieldHOffset, posY, fieldWidth + fieldVOffset, posY);
                posY += oneCellVSpace + (int)cellPen.Width;
            }

            float pointSpace = fieldWidth / values.Count;
            float procentSize = (fieldHeight - 1) / 100;
            var points = new PointF[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                var y1 = (int)(values[i] * procentSize);
                points[i] = new PointF(pointSpace * i + fieldHOffset, pbMain.Height - y1 - 2);
            }
            graphics.DrawLines(dataPen, points);

            var font = new Font("Consolas", 9);

            graphics.DrawString(
                name,
                font, textBrush,
                new RectangleF(0, -2, pbMain.Width, 15),
                new StringFormat() { Alignment = StringAlignment.Near });

            graphics.DrawString(
                string.Format("{0}:{1,3}{2}", valueName, Math.Round(values[59]), valueUnit),
                font, textBrush,
                new RectangleF(0, -2 + 10, pbMain.Width, 15),
                new StringFormat() { Alignment = StringAlignment.Near });

            graphics.DrawString(
                string.Format("Min:{0,3}{1}", Math.Round(min), valueUnit),
                font, textBrush,
                new RectangleF(0, -2, pbMain.Width, 15),
                new StringFormat() { Alignment = StringAlignment.Far });

            graphics.DrawString(
                string.Format("Max:{0,3}{1}", Math.Round(max), valueUnit),
                font, textBrush,
                new RectangleF(0, -2 + 10, pbMain.Width, 15),
                new StringFormat() { Alignment = StringAlignment.Far });
        }

        public void AddValue(float value)
        {
            values.RemoveAt(0);
            values.Add(value);
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            pbMain.Refresh();
        }

        public void SetName(string name, ValueType type)
        {
            this.name = name;
            switch (type)
            {
                case ValueType.Load:
                    valueName = "Load";
                    valueUnit = "%";
                    break;

                case ValueType.Temp:
                    valueName = "Temp";
                    valueUnit = "°C";
                    break;

            }
        }
    }
}
