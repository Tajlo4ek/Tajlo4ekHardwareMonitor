using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tajlo4ekHardwareMonitor
{
    internal class ControlPlaceManager
    {
        public enum PositionH
        {
            Right,
            Left
        }
        public enum PositionV
        {
            Top,
            Bottom
        }

        private class Data
        {
            public Control parent;
            public Control child;
            public PositionH positionH;
            public PositionV positionV;
            public int margin;

            public override string ToString()
            {
                return string.Format("parent:{0} child:{1}", parent, child);
            }
        }

        private static readonly List<Data> controls = new List<Data>();

        private ControlPlaceManager() { }

        public static void Connect(Control parent, Control child, PositionH posH, PositionV posV, int margin)
        {
            var can = CheckCanConnect(parent, child);

            if (can)
            {
                controls.Add(new Data()
                {
                    parent = parent,
                    child = child,
                    positionH = posH,
                    positionV = posV,
                    margin = margin
                });

                parent.SizeChanged += ParentChanged;
                parent.LocationChanged += ParentChanged;
            }
        }

        private static void ParentChanged(object sender, EventArgs e)
        {
            foreach (var data in GetChildByParent((Control)sender))
            {
                int childPosX = data.positionH == PositionH.Left ? data.parent.Left : data.parent.Right + data.margin;
                int childPosY = data.positionV == PositionV.Top ? data.parent.Top : data.parent.Bottom + data.margin;

                data.child.Location = new System.Drawing.Point(childPosX, childPosY);
            }
        }

        private static IEnumerable<Data> GetChildByParent(Control parent)
        {
            return controls.Where((data) => { return data.parent == parent; });
        }

        private static bool CheckCanConnect(Control parent, Control child)
        {
            if (controls.Where((data) => { return data.child == child; }).Count() != 0)
            {
                throw new Exception("double connect");
            }

            List<Control> buf = new List<Control>() { parent };

            while (buf.Count > 0)
            {
                var now = buf[0];
                buf.RemoveAt(0);

                foreach (var data in controls)
                {
                    if (data.child == now)
                    {
                        buf.Add(data.parent);
                    };
                }

                if (now == child)
                {
                    throw new Exception("cycle connect");
                }
            }

            return true;
        }
    }
}
