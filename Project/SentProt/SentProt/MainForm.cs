using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using SentProt.ClientSocket;
using SentProt.ClientSocket.AppBase;
using CommonUtils.Logger;
using WindowsFormTelerik.ControlCommon;

namespace SentProt
{
    /*
     * 接收STENT信号
     * 1）请求一次，接收一次数据
     * 2）主界面滚动显示所有信号
     *      可设置界面显示数量
     *      可导出数据
     *      将所有数据log
     * 3）快信号：可设置快信号的高低位顺序
     *      显示最新一条数据或显示某一条数据
     * 4）慢信号：显示桢数据的类别，如标准桢/扩展桢，需要解析一包完整数据后才能判断
     *      滚动显示
     */
    public partial class MainForm : RadForm
    {
        private int revCount;
        private Object obj = new object();
        private delegate void Actions(MyPackageInfo packageInfo);
        private Actions actions;
        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Init();
            EventHandlers();
        }

        private void EventHandlers()
        {
            actions = new Actions(RefreshGridData);
            this.menu_connectServer.Click += Menu_connectServer_Click;
            this.tool_connectServer.Click += Tool_connectServer_Click;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
        }

        private void Tool_connectServer_Click(object sender, EventArgs e)
        {
            ConnectServerView();
        }

        private void SuperEasyClient_NoticeMessageEvent(MyPackageInfo packageInfo)
        {
            this.grid_stentCompleteSignal.BeginInvoke(actions, packageInfo);
        }

        private void Menu_connectServer_Click(object sender, EventArgs e)
        {
            ConnectServerView();
        }

        private void RefreshGridData(MyPackageInfo packageInfo)
        {
            AnalysisUsualSignal(packageInfo);
            AnalysisSlowSignal(packageInfo);
            AnalysisQuickSignal(packageInfo);
        }

        private void AnalysisUsualSignal(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length  / 8;
            for (int i = 0; i < count; i += 8)
            {
                var iData = "" + packageInfo.Data[i] + packageInfo.Data[i + 1] + packageInfo.Data[i + 2] + packageInfo.Data[i + 3] + packageInfo.Data[i + 4] + packageInfo.Data[i + 5] + packageInfo.Data[i + 6] + packageInfo.Data[i + 7];
                this.grid_stentCompleteSignal.Rows.AddNew();
                this.grid_stentCompleteSignal.Rows[revCount].Cells[0].Value = revCount + 1;
                this.grid_stentCompleteSignal.Rows[revCount].Cells[1].Value = packageInfo.Data.Length;
                this.grid_stentCompleteSignal.Rows[revCount].Cells[2].Value = BitConverter.ToString(packageInfo.Data);
                this.grid_stentCompleteSignal.Rows[revCount].IsSelected = true;
                this.grid_stentCompleteSignal.TableElement.ScrollToRow(this.grid_stentCompleteSignal.Rows.Count);
                revCount++;
            }
        }

        private void AnalysisSlowSignal(MyPackageInfo packageInfo)
        {
            //慢信号有两种类型：标准帧与扩展帧
            //先解析出前6位数据，判断出帧类型
        }

        /*
         * 【快信号】
         * 1）显示最新数据
         * 2）根据高低位顺序，重新计算数据
         */
        private void AnalysisQuickSignal(MyPackageInfo packageInfo)
        {

        }

        private void ConnectServerView()
        {
            AddConnection addConnection = new AddConnection();
            addConnection.ShowDialog();
        }

        private void Init()
        {
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentCompleteSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentSlowSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickSignal,false);
            this.grid_stentCompleteSignal.ReadOnly = true;
            this.grid_stentQuickSignal.ReadOnly = true;
            this.grid_stentSlowSignal.ReadOnly = true;
        }
    }
}
