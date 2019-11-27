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
        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Init();
            EventHandlers();
        }

        private void EventHandlers()
        {
            this.menu_connectServer.Click += Menu_connectServer_Click;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
        }

        private void SuperEasyClient_NoticeMessageEvent(MyPackageInfo packageInfo)
        {
            this.grid_stentCompleteSignal.Rows.AddNew();
            this.grid_stentCompleteSignal.Rows[revCount].Cells[0].Value = revCount + 1;
            this.grid_stentCompleteSignal.Rows[revCount].Cells[1].Value = packageInfo.Data.Length;
            this.grid_stentCompleteSignal.Rows[revCount].Cells[2].Value = BitConverter.ToString(packageInfo.Data);
            revCount++;
        }

        private void Menu_connectServer_Click(object sender, EventArgs e)
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
