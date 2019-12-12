using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StentDevice
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.listView1.Columns.Add("order");
            this.listView1.Columns.Add("status");
            this.listView1.Columns.Add("data1");
            this.listView1.GridLines = true;
            this.listView1.FullRowSelect = true;
            this.listView1.View = View.Details;
            this.listView1.Scrollable = true;
            this.listView1.MultiSelect = false;
            this.listView1.HeaderStyle = ColumnHeaderStyle.Clickable;
        }

        private void RefreshData()
        {
            for (int i = 0; i < 1000; i++)
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = (i + 1).ToString();
                listViewItem.SubItems.Add("X2_"+i);
                this.listView1.Items.Add(listViewItem);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}
