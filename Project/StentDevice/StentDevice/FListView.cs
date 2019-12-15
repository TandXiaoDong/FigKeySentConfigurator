using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StentDevice
{
    class FListView : ListView
    {
        public FListView()
        {
            SetStyle(ControlStyles.DoubleBuffer |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles(); 
        }
    }
}
