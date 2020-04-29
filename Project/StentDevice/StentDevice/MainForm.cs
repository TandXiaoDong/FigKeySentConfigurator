using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using StentDevice.ClientSocket;
using StentDevice.ClientSocket.AppBase;
using CommonUtils.Logger;
using CommonUtils.DEncrypt;
using WindowsFormTelerik.ControlCommon;
using CommonUtils.ByteHelper;
using CommonUtils.FileHelper;
using CommonUtils.SCVFile;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Reflection;
using WindowsFormTelerik.GridViewExportData;
using WindowsFormTelerik.CommonUI;
using StentDevice.Model;
using Excel = Microsoft.Office.Interop.Excel;

namespace StentDevice
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
        #region 公有变量
        private string serverIP;
        private int serverPort;
        private string stentConfigDirectory;
        private int cacheFrameNumber = 10000;//显示帧数
        #endregion

        #region 常量
        private const string STENT_CONFIG_FILE = "stentConfig.ini";
        private const string STENT_CONFIG_SECTION = "STENT";
        private const string STENT_CONFIG_SECTION_CH1 = "CHANNEL1";
        private const string STENT_CONFIG_SECTION_CH2 = "CHANNEL2";
        private const string STENT_CONFIG_FRAME_COUNT_KEY = "frameCount";
        private const string STENT_CONFIG_IS_AUTO_KEY = "IsAutoSend";
        private const string STENT_CONFIG_TIME_INTERNAL = "autoSendTimeInternal";
        private const string STENT_CONFIG_SERVER_URL = "serverIP";
        private const string STENT_CONFIG_SERVER_PORT = "port";
        #endregion

        private System.Timers.Timer timerCh1;
        private System.Timers.Timer timerCh2;

        private ChannelData channelDataCh1;
        private ChannelData channelDataCh2;

        private List<ListViewItem> cacheListViewSourceCompleteCh1;
        private List<ListViewItem> cacheListViewSourceSlowCh1;
        private List<ListViewItem> cacheListViewSourceQuickCh1;

        private List<ListViewItem> cacheListViewSourceCompleteCh2;
        private List<ListViewItem> cacheListViewSourceSlowCh2;
        private List<ListViewItem> cacheListViewSourceQuickCh2;

        //缓存LISTVIEW 每包数据，加入到虚模式缓存总数
        private List<ListViewItem> cacheListPerpTempCompleteCh1;
        private List<ListViewItem> cacheListPerpTempSlowCh1;
        private List<ListViewItem> cacheListPerpTempQuickCh1;

        private List<ListViewItem> cacheListPerpTempCompleteCh2;
        private List<ListViewItem> cacheListPerpTempSlowCh2;
        private List<ListViewItem> cacheListPerpTempQuickCh2;

        //解析的每一包数据在界面显示的实际数量
        private int cacheActualCountCompleteCh1;
        private int cacheActualCountSlowCh1;
        private int cacheActualCountQuickCh1;

        private int cacheActualCountCompleteCh2;
        private int cacheActualCountSlowCh2;
        private int cacheActualCountQuickCh2;

        private int cacheCountCh1 = 1;
        private int cacheCountCh2 = 1;
        private int cacheCountCh3 = 1;
        private int cacheCountCh4 = 1;

        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Init();
            CheckRegister();
            InitListView();
            EventHandlers();
            this.radDock1.ActiveWindow = this.documentChannel1;
            this.tool_disconnect.Enabled = false;
        }

        private void SetControlDoubleBuffer()
        {
            //设置窗体的双缓冲
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
            //GridDoubleBuffer.SetDoubleBuffered(this.grid_stentCompleteSignalCh1,true);
        }

        private void InitListView()
        {
            cacheListViewSourceCompleteCh1 = new List<ListViewItem>();
            cacheListViewSourceSlowCh1 = new List<ListViewItem>();
            cacheListViewSourceQuickCh1 = new List<ListViewItem>();

            cacheListViewSourceSlowCh2 = new List<ListViewItem>();
            cacheListViewSourceCompleteCh2 = new List<ListViewItem>();
            cacheListViewSourceQuickCh2 = new List<ListViewItem>();

            cacheListPerpTempCompleteCh1 = new List<ListViewItem>();
            cacheListPerpTempSlowCh1 = new List<ListViewItem>();
            cacheListPerpTempQuickCh1 = new List<ListViewItem>();

            cacheListPerpTempCompleteCh2 = new List<ListViewItem>();
            cacheListPerpTempSlowCh2 = new List<ListViewItem>();
            cacheListPerpTempQuickCh2 = new List<ListViewItem>();

            #region grid_stentCompleteSignalCh1
            //this.grid_stentCompleteSignalCh1.Columns.Add("序号");
            this.grid_stentCompleteSignalCh1.Columns.Add("STATUS");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA1");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA2");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA3");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA4");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA5");
            this.grid_stentCompleteSignalCh1.Columns.Add("DATA6");
            this.grid_stentCompleteSignalCh1.Columns.Add("CRC");
            this.grid_stentCompleteSignalCh1.Columns.Add("CAL_CRC"); 

            this.grid_stentCompleteSignalCh1.GridLines = false;
            this.grid_stentCompleteSignalCh1.FullRowSelect = true;
            this.grid_stentCompleteSignalCh1.View = View.Details;
            this.grid_stentCompleteSignalCh1.Scrollable = true;
            this.grid_stentCompleteSignalCh1.MultiSelect = false;
            this.grid_stentCompleteSignalCh1.HeaderStyle = ColumnHeaderStyle.Clickable;

            this.grid_stentCompleteSignalCh1.Columns[0].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[1].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[2].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[3].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[4].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[5].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[6].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[7].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            this.grid_stentCompleteSignalCh1.Columns[8].Width = this.grid_stentCompleteSignalCh1.Width / 9 - 10;
            //this.grid_stentCompleteSignalCh1.Columns[9].Width = this.grid_stentCompleteSignalCh1.Width / 10;

            //this.grid_stentCompleteSignalCh1.VirtualMode = true;
            #endregion

            #region grid_stentSlowSignalCh1
            //this.grid_stentSlowSignalCh1.Columns.Add("序号");
            this.grid_stentSlowSignalCh1.Columns.Add("帧类型");
            this.grid_stentSlowSignalCh1.Columns.Add("MessageID");
            this.grid_stentSlowSignalCh1.Columns.Add("DATA"); 
            //this.grid_stentSlowSignalCh1.Columns.Add("CRC");

            this.grid_stentSlowSignalCh1.GridLines = false;
            this.grid_stentSlowSignalCh1.FullRowSelect = true;
            this.grid_stentSlowSignalCh1.View = View.Details;
            this.grid_stentSlowSignalCh1.Scrollable = true;
            this.grid_stentSlowSignalCh1.MultiSelect = false;
            this.grid_stentSlowSignalCh1.HeaderStyle = ColumnHeaderStyle.Clickable;

            this.grid_stentSlowSignalCh1.Columns[0].Width = this.grid_stentSlowSignalCh1.Width / 3 - 10;
            this.grid_stentSlowSignalCh1.Columns[1].Width = this.grid_stentSlowSignalCh1.Width / 3 - 10;
            this.grid_stentSlowSignalCh1.Columns[2].Width = this.grid_stentSlowSignalCh1.Width / 3 - 10;
            //this.grid_stentSlowSignalCh1.Columns[3].Width = this.grid_stentSlowSignalCh1.Width / 4;

            this.grid_stentSlowSignalCh1.VirtualMode = true;
            #endregion

            #region grid_stentQuickBothCh1
            this.grid_stentQuickBothCh1.Columns.Add("DATA1");
            this.grid_stentQuickBothCh1.Columns.Add("DATA2");

            this.grid_stentQuickBothCh1.GridLines = false;
            this.grid_stentQuickBothCh1.FullRowSelect = true;
            this.grid_stentQuickBothCh1.View = View.Details;
            this.grid_stentQuickBothCh1.Scrollable = true;
            this.grid_stentQuickBothCh1.MultiSelect = false;
            this.grid_stentQuickBothCh1.HeaderStyle = ColumnHeaderStyle.Clickable;
            this.grid_stentQuickBothCh1.Columns[0].Width = this.grid_stentQuickBothCh1.Width / 2 - 10;
            this.grid_stentQuickBothCh1.Columns[1].Width = this.grid_stentQuickBothCh1.Width / 2 - 10;
            //this.grid_stentQuickBothCh1.VirtualMode = true;
            #endregion

            #region grid_stentCompleteSignalCh2
            //this.grid_stentCompleteSignalCh2.Columns.Add("序号");
            this.grid_stentCompleteSignalCh2.Columns.Add("STATUS");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA1");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA2");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA3");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA4");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA5");
            this.grid_stentCompleteSignalCh2.Columns.Add("DATA6");
            this.grid_stentCompleteSignalCh2.Columns.Add("CRC");
            this.grid_stentCompleteSignalCh2.Columns.Add("CAL_CRC");

            this.grid_stentCompleteSignalCh2.GridLines = false;
            this.grid_stentCompleteSignalCh2.FullRowSelect = true;
            this.grid_stentCompleteSignalCh2.View = View.Details;
            this.grid_stentCompleteSignalCh2.Scrollable = true;
            this.grid_stentCompleteSignalCh2.MultiSelect = false;
            this.grid_stentCompleteSignalCh2.HeaderStyle = ColumnHeaderStyle.Clickable;

            this.grid_stentCompleteSignalCh2.Columns[0].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[1].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[2].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[3].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[4].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[5].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[6].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[7].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            this.grid_stentCompleteSignalCh2.Columns[8].Width = this.grid_stentCompleteSignalCh2.Width / 9 - 10;
            //this.grid_stentCompleteSignalCh2.Columns[9].Width = this.grid_stentCompleteSignalCh2.Width / 10;

            this.grid_stentCompleteSignalCh2.VirtualMode = true;
            #endregion

            #region grid_stentSlowSignalCh2
            //this.grid_stentSlowSignalCh2.Columns.Add("序号");
            this.grid_stentSlowSignalCh2.Columns.Add("帧类型");
            this.grid_stentSlowSignalCh2.Columns.Add("MessageID");
            this.grid_stentSlowSignalCh2.Columns.Add("DATA");
            //this.grid_stentSlowSignalCh2.Columns.Add("CRC");

            this.grid_stentSlowSignalCh2.GridLines = false;
            this.grid_stentSlowSignalCh2.FullRowSelect = true;
            this.grid_stentSlowSignalCh2.View = View.Details;
            this.grid_stentSlowSignalCh2.Scrollable = true;
            this.grid_stentSlowSignalCh2.MultiSelect = false;
            this.grid_stentSlowSignalCh2.HeaderStyle = ColumnHeaderStyle.Clickable;

            this.grid_stentSlowSignalCh2.Columns[0].Width = this.grid_stentSlowSignalCh2.Width / 3 - 10;
            this.grid_stentSlowSignalCh2.Columns[1].Width = this.grid_stentSlowSignalCh2.Width / 3 - 10;
            this.grid_stentSlowSignalCh2.Columns[2].Width = this.grid_stentSlowSignalCh2.Width / 3 - 10;
            //this.grid_stentSlowSignalCh2.Columns[3].Width = this.grid_stentSlowSignalCh2.Width / 4;

            this.grid_stentSlowSignalCh2.VirtualMode = true;
            #endregion

            #region grid_stentQuickBothCh2
            this.grid_stentQuickBothCh2.Columns.Add("DATA1");
            this.grid_stentQuickBothCh2.Columns.Add("DATA2");

            this.grid_stentQuickBothCh2.GridLines = false;
            this.grid_stentQuickBothCh2.FullRowSelect = true;
            this.grid_stentQuickBothCh2.View = View.Details;
            this.grid_stentQuickBothCh2.Scrollable = true;
            this.grid_stentQuickBothCh2.MultiSelect = false;
            this.grid_stentQuickBothCh2.HeaderStyle = ColumnHeaderStyle.Clickable;
            this.grid_stentQuickBothCh2.Columns[0].Width = this.grid_stentQuickBothCh2.Width / 2 - 10;
            this.grid_stentQuickBothCh2.Columns[1].Width = this.grid_stentQuickBothCh2.Width / 2 - 10;

            //this.grid_stentQuickBothCh2.VirtualMode = true;
            #endregion
        }

        #region Reset virtual data source

        public void ReSetCompleteCh1(IList<ListViewItem> list)
        {
            try
            {
                this.grid_stentCompleteSignalCh1.VirtualMode = true;
                foreach (var item in list)
                {
                    this.cacheListViewSourceCompleteCh1.Add(item);
                    cacheActualCountCompleteCh1++;
                }
                //判断是否超过最大显示数量
                if (this.cacheListViewSourceCompleteCh1.Count >= this.cacheFrameNumber)
                {
                    int delRow = this.cacheListViewSourceCompleteCh1.Count - this.cacheFrameNumber;
                    this.cacheListViewSourceCompleteCh1.RemoveRange(0, delRow);

                    //int i = 0;
                    //foreach (ListViewItem lvItem in this.cacheListViewSourceCompleteCh1)
                    //{
                    //    lvItem.SubItems[0].Text = (i + 1).ToString();
                    //    i++;
                    //}
                    this.grid_stentCompleteSignalCh1.Refresh();
                    Application.DoEvents();
                }
                this.cacheListPerpTempCompleteCh1.Clear();
                this.grid_stentCompleteSignalCh1.VirtualListSize = this.cacheListViewSourceCompleteCh1.Count;
                this.grid_stentCompleteSignalCh1.Items[this.cacheListViewSourceCompleteCh1.Count -1].EnsureVisible();
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("reset ch1 complete "+ex.Message+ex.StackTrace);
            }
        }

        public void ReSetSlowCh1(IList<ListViewItem> list)
        {
            try
            {
                this.grid_stentSlowSignalCh1.VirtualMode = true;
                foreach (var item in list)
                {
                    this.cacheListViewSourceSlowCh1.Add(item);
                    cacheActualCountSlowCh1++;
                }
                //判断是否超过最大显示数量
                if (this.cacheListViewSourceSlowCh1.Count >= this.cacheFrameNumber)
                {
                    int delRow = this.cacheListViewSourceSlowCh1.Count - this.cacheFrameNumber;
                    this.cacheListViewSourceSlowCh1.RemoveRange(0, delRow);
                    //int i = 0;
                    //foreach (var lvItem in this.cacheListViewSourceSlowCh1)
                    //{
                    //    lvItem.SubItems[0].Text = (i + 1).ToString();
                    //    i++;
                    //}
                    this.grid_stentSlowSignalCh1.Refresh();
                }
                cacheListPerpTempSlowCh1.Clear();
                this.grid_stentSlowSignalCh1.VirtualListSize = this.cacheListViewSourceSlowCh1.Count;
                this.grid_stentSlowSignalCh1.Items[this.cacheListViewSourceSlowCh1.Count - 1].EnsureVisible();
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("reset ch1 slow " + ex.Message + ex.StackTrace);
            }
        }

        public void ReSetQuickCh1(IList<ListViewItem> list)
        {
            this.grid_stentQuickBothCh1.VirtualMode = true;
            foreach (var item in list)
            {
                this.cacheListViewSourceQuickCh1.Add(item);
                cacheActualCountQuickCh1++;
            }
            cacheListPerpTempQuickCh1.Clear();
            this.grid_stentQuickBothCh1.VirtualListSize = this.cacheListViewSourceQuickCh1.Count;
        }

        public void ReSetCompleteCh2(IList<ListViewItem> list)
        {
            this.grid_stentCompleteSignalCh2.VirtualMode = true;
            foreach (var item in list)
            {
                this.cacheListViewSourceCompleteCh2.Add(item);
                cacheActualCountCompleteCh2++;
            }
            //判断是否超过最大显示数量
            if (this.cacheListViewSourceCompleteCh2.Count >= this.cacheFrameNumber)
            {
                int delRow = this.cacheListViewSourceCompleteCh2.Count - this.cacheFrameNumber;
                this.cacheListViewSourceCompleteCh2.RemoveRange(0, delRow);
                int i = 0;
                //foreach (var lvItem in this.cacheListViewSourceCompleteCh2)
                //{
                //    lvItem.SubItems[0].Text = (i + 1).ToString();
                //    i++;
                //}
                this.grid_stentCompleteSignalCh2.Refresh();
            }
            cacheListPerpTempCompleteCh2.Clear();
            this.grid_stentCompleteSignalCh2.VirtualListSize = this.cacheListViewSourceCompleteCh2.Count;
        }

        public void ReSetSlowCh2(IList<ListViewItem> list)
        {
            this.grid_stentSlowSignalCh2.VirtualMode = true;
            foreach (var item in list)
            {
                this.cacheListViewSourceSlowCh2.Add(item);
                cacheActualCountSlowCh2++;
            }
            //判断是否超过最大显示数量
            if (this.cacheListViewSourceSlowCh2.Count >= this.cacheFrameNumber)
            {
                int delRow = this.cacheListViewSourceSlowCh2.Count - this.cacheFrameNumber;
                this.cacheListViewSourceSlowCh2.RemoveRange(0, delRow);
                int i = 0;
                //foreach (var lvItem in this.cacheListViewSourceSlowCh2)
                //{
                //    lvItem.SubItems[0].Text = (i + 1).ToString();
                //    i++;
                //}
                this.grid_stentSlowSignalCh2.Refresh();
            }
            this.cacheListPerpTempSlowCh2.Clear();
            this.grid_stentSlowSignalCh2.VirtualListSize = this.cacheListViewSourceSlowCh2.Count;
        }

        public void ReSetQuickCh2(IList<ListViewItem> list)
        {
            this.grid_stentQuickBothCh2.VirtualMode = true;
            foreach (var item in list)
            {
                this.cacheListViewSourceQuickCh2.Add(item);
                cacheActualCountQuickCh2++;
            }
            cacheListPerpTempQuickCh2.Clear();
            this.grid_stentQuickBothCh2.VirtualListSize = this.cacheListViewSourceQuickCh2.Count;
        }

        #endregion

        private void EventHandlers()
        {
            timerCh1 = new System.Timers.Timer();
            timerCh2 = new System.Timers.Timer();
            timerCh1.Elapsed += TimerCh1_Elapsed;
            timerCh2.Elapsed += TimerCh2_Elapsed;

            #region this is menu item event
            this.menu_connectServer.Click += Menu_connectServer_Click;
            this.menu_exit.Click += Menu_exit_Click;
            this.menu_disconnect.Click += Menu_disconnect_Click;
            //this.menu_channel1.Click += Menu_channel1_Click;
            //this.menu_channel2.Click += Menu_channel2_Click;
            //this.menu_allChannel.Click += Menu_allChannel_Click;
            #endregion

            #region this is tool strip event
            this.tool_connectServer.Click += Tool_connectServer_Click;
            this.tool_disconnect.Click += Tool_disconnect_Click;
            //this.tool_continue.Click += Tool_continue_Click;
            //this.tool_pause.Click += Tool_pause_Click;
            this.tool_cacheFrameAmount.Click += Tool_cacheFrameAmount_Click;
            this.tool_help.Click += Tool_help_Click;
            this.tool_channel1Send.Click += Tool_channel1Send_Click;
            this.tool_channel2Send.Click += Tool_channel2Send_Click;
            this.tool_channel1stop.Click += Tool_channel1stop_Click;
            this.tool_channel2Stop.Click += Tool_channel2Stop_Click;
            this.tool_channel1Clear.Click += Tool_channel1Clear_Click;
            this.tool_channel2Clear.Click += Tool_channel2Clear_Click;
            this.tool_channel1AutoSend.Click += Tool_channel1AutoSend_Click;
            this.tool_channel2AutoSend.Click += Tool_channel2AutoSend_Click;

            this.tool_channel1Export.Click += Tool_channel1Export_Click;
            this.tool_ch1ExportSlow.Click += Tool_ch1ExportSlow_Click;
            this.tool_ch1ExportQuick.Click += Tool_ch1ExportQuick_Click;

            this.tool_channel2Export.Click += Tool_channel2Export_Click;
            this.tool_ch2ExportSlow.Click += Tool_ch2ExportSlow_Click;
            this.tool_ch2ExportQuick.Click += Tool_ch2ExportQuick_Click;
            #endregion

            #region this is listView event
            this.grid_stentCompleteSignalCh1.RetrieveVirtualItem += Grid_stentCompleteSignalCh1_RetrieveVirtualItem;
            this.grid_stentSlowSignalCh1.RetrieveVirtualItem += Grid_stentSlowSignalCh1_RetrieveVirtualItem;
            this.grid_stentQuickBothCh1.RetrieveVirtualItem += Grid_stentQuickBothCh1_RetrieveVirtualItem;
            this.grid_stentCompleteSignalCh2.RetrieveVirtualItem += Grid_stentCompleteSignalCh2_RetrieveVirtualItem;
            this.grid_stentSlowSignalCh2.RetrieveVirtualItem += Grid_stentSlowSignalCh2_RetrieveVirtualItem;
            this.grid_stentQuickBothCh2.RetrieveVirtualItem += Grid_stentQuickBothCh2_RetrieveVirtualItem;
            #endregion

            this.rb_highBefore1Ch1.CheckStateChanged += Rb_highBefore_CheckStateChanged;
            this.rb_highBefore2Ch1.CheckStateChanged += Rb_highBefore2_CheckStateChanged;
            this.rb_highBefore1Ch2.CheckStateChanged += Rb_highBefore1Ch2_CheckStateChanged;
            this.rb_highBefore2Ch2.CheckStateChanged += Rb_highBefore2Ch2_CheckStateChanged;
            SuperEasyClient.NoticeConnectEvent += SuperEasyClient_NoticeConnectEvent;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            this.FormClosed += MainForm_FormClosed;

            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
            //this.grid_stentCompleteSignalCh1.CellValuePushed += Grid_stentCompleteSignalCh1_CellValuePushed;
            //this.grid_stentCompleteSignalCh1.CellValueNeeded += Grid_stentCompleteSignalCh1_CellValueNeeded;
            //e.Value = this.dtCh1.Rows[e.RowIndex][e.ColumnIndex].ToString();
        }

        #region listView RetireVirtualItem event

        private void Grid_stentQuickBothCh2_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.cacheListViewSourceQuickCh2 == null || this.cacheListViewSourceQuickCh2.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.cacheListViewSourceQuickCh2[e.ItemIndex];
            e.Item = lv;
        }

        private void Grid_stentSlowSignalCh2_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.cacheListViewSourceSlowCh2 == null || this.cacheListViewSourceSlowCh2.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.cacheListViewSourceSlowCh2[e.ItemIndex];
            e.Item = lv;
        }

        private void Grid_stentCompleteSignalCh2_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.cacheListViewSourceCompleteCh2 == null || this.cacheListViewSourceCompleteCh2.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.cacheListViewSourceCompleteCh2[e.ItemIndex];
            e.Item = lv;
        }

        private void Grid_stentQuickBothCh1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.cacheListViewSourceQuickCh1 == null || this.cacheListViewSourceQuickCh1.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.cacheListViewSourceQuickCh1[e.ItemIndex];
            e.Item = lv;
        }

        private void Grid_stentSlowSignalCh1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                if (this.cacheListViewSourceSlowCh1 == null || this.cacheListViewSourceSlowCh1.Count == 0)
                {
                    return;
                }
                ListViewItem lv = this.cacheListViewSourceSlowCh1[e.ItemIndex];
                e.Item = lv;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ch1slow " + ex.Message + ex.StackTrace);
            }
        }

        private void Grid_stentCompleteSignalCh1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                if (this.cacheListViewSourceCompleteCh1 == null || this.cacheListViewSourceCompleteCh1.Count == 0)
                {
                    return;
                }
                ListViewItem lv = this.cacheListViewSourceCompleteCh1[e.ItemIndex];
                e.Item = lv;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ch1complete " + ex.Message + ex.StackTrace + " "+e.ItemIndex);
            }
        }

        #endregion

        private void Menu_allChannel_Click(object sender, EventArgs e)
        {
            
        }

        private void Menu_channel2_Click(object sender, EventArgs e)
        {
            this.radDock1.ActiveWindow = this.documentChannel2;
        }

        private void Menu_channel1_Click(object sender, EventArgs e)
        {
            //this.radDock1.AddDocument(this.documentChannel1);

            this.radDock1.AddDocument(documentChannel1);
            this.documentChannel1.Show();
        }

        private void TimerCh1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timerCh1.Interval = channelDataCh1.AutoSendTimeInternal;
            SendMessageCh1();
        }

        private void TimerCh2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timerCh2.Interval = channelDataCh2.AutoSendTimeInternal;
            SendMessageCh2();
        }

        private void Tool_ch2ExportQuick_Click(object sender, EventArgs e)
        {
            var saveFileName = "quickSignalData_ch2_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "DATA1,DATA2";
            if (CsvHelper.SaveListViewSCV(this.grid_stentQuickBothCh2, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Tool_ch2ExportSlow_Click(object sender, EventArgs e)
        {
            var saveFileName = "slowSignalData_ch2_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "帧类型,MessageID,DATA";
            if (CsvHelper.SaveListViewSCV(this.grid_stentSlowSignalCh2, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Tool_ch1ExportQuick_Click(object sender, EventArgs e)
        {
            var saveFileName = "quickSignalData_ch1_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "DATA1,DATA2";
            if (CsvHelper.SaveListViewSCV(this.grid_stentQuickBothCh1, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Tool_ch1ExportSlow_Click(object sender, EventArgs e)
        {
            var saveFileName = "slowSignalData_ch1_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "帧类型,MessageID,DATA";
            if (CsvHelper.SaveListViewSCV(this.grid_stentSlowSignalCh1, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Tool_channel2Export_Click(object sender, EventArgs e)
        {
            var saveFileName = "signalData_ch2_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "STATUS,DATA1,DATA2,DATA3,DATA4,DATA5,DATA6,CRC,CAL_CRC";
            if (CsvHelper.SaveListViewSCV(this.grid_stentCompleteSignalCh2, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Tool_channel1Export_Click(object sender, EventArgs e)
        {
            var saveFileName = "signalData_ch1_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var columns = "STATUS,DATA1,DATA2,DATA3,DATA4,DATA5,DATA6,CRC,CAL_CRC";
            if (CsvHelper.SaveListViewSCV(this.grid_stentCompleteSignalCh1, saveFileName, "", columns))
                MessageBox.Show("数据导出完成！","INFO",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void Tool_channel2AutoSend_Click(object sender, EventArgs e)
        {
            SetAutoSendSet();
        }

        private void Tool_channel1AutoSend_Click(object sender, EventArgs e)
        {
            SetAutoSendSet();
        }

        private void Tool_channel2Clear_Click(object sender, EventArgs e)
        {
            //this.grid_stentCompleteSignalCh2.VirtualMode = false;
            this.cacheListViewSourceCompleteCh2.Clear();
            this.grid_stentCompleteSignalCh2.VirtualListSize = this.cacheListViewSourceCompleteCh2.Count;

            //this.grid_stentSlowSignalCh2.VirtualMode = false;
            this.cacheListViewSourceSlowCh2.Clear();
            this.grid_stentSlowSignalCh2.VirtualListSize = this.cacheListViewSourceSlowCh2.Count;

            //this.grid_stentQuickBothCh2.VirtualMode = false;
            this.grid_stentQuickBothCh2.Items.Clear();

            channelDataCh2.RevCount = 0;
            channelDataCh2.SlowSignalCount = 0;
        }

        private void Tool_channel1Clear_Click(object sender, EventArgs e)
        {
            //this.grid_stentCompleteSignalCh1.VirtualMode = false;
            this.cacheListViewSourceCompleteCh1.Clear();
            this.grid_stentCompleteSignalCh1.VirtualListSize = this.cacheListViewSourceCompleteCh1.Count;

            //this.grid_stentSlowSignalCh1.VirtualMode = false;
            this.cacheListViewSourceSlowCh1.Clear();
            this.grid_stentSlowSignalCh1.VirtualListSize = this.cacheListViewSourceSlowCh1.Count;

            //this.grid_stentQuickBothCh1.VirtualMode = false;
            this.grid_stentQuickBothCh1.Items.Clear();

            channelDataCh1.RevCount = 0;
            channelDataCh1.SlowSignalCount = 0;
        }

        private void Tool_channel2Stop_Click(object sender, EventArgs e)
        {
            //停止请求数据
            timerCh2.Stop();
            this.tool_channel2Send.Enabled = true;
            this.tool_channel2Stop.Enabled = false;
            this.tool_channel2Export.Enabled = true;
            this.tool_ch2ExportQuick.Enabled = true;
            this.tool_ch2ExportSlow.Enabled = true;
        }

        private void Tool_channel1stop_Click(object sender, EventArgs e)
        {
            //停止请求数据
            timerCh1.Stop();
            this.tool_channel1Send.Enabled = true;
            this.tool_channel1stop.Enabled = false;
            this.tool_channel1Export.Enabled = true;
            this.tool_ch1ExportQuick.Enabled = true;
            this.tool_ch1ExportSlow.Enabled = true;
        }

        private void Tool_channel2Send_Click(object sender, EventArgs e)
        {
            //SendMessage(StentSignalEnum.RequestDataCh2);
            timerCh2.Start();
            this.tool_channel2Send.Enabled = false;
            this.tool_channel2Stop.Enabled = true;
            this.tool_channel2Export.Enabled = false;
            this.tool_ch2ExportQuick.Enabled = false;
            this.tool_ch2ExportSlow.Enabled = false;
        }

        private void Tool_channel1Send_Click(object sender, EventArgs e)
        {
            timerCh1.Start();
            //SendMessage(StentSignalEnum.RequestDataCh1);
            this.tool_channel1Send.Enabled = false;
            this.tool_channel1stop.Enabled = true;
            this.tool_channel1Export.Enabled = false;
            this.tool_ch1ExportQuick.Enabled = false;
            this.tool_ch1ExportSlow.Enabled = false;
        }

        private void SendMessage(StentSignalEnum stentSignalEnum)
        {
            if (SuperEasyClient.client == null)
                return;
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求发送数据
            SuperEasyClient.SendMessage(stentSignalEnum, new byte[] { });
            //ch1
            if (stentSignalEnum == StentSignalEnum.RequestDataCh1)
            {
                this.tool_channel1Send.Enabled = false;
                this.tool_channel1stop.Enabled = true;
                this.tool_channel1Export.Enabled = false;
                this.tool_ch1ExportQuick.Enabled = false;
                this.tool_ch1ExportSlow.Enabled = false;
            }
            //ch2
            if (stentSignalEnum == StentSignalEnum.RequestDataCh2)
            {
                this.tool_channel2Send.Enabled = false;
                this.tool_channel2Stop.Enabled = true;
                this.tool_channel2Export.Enabled = false;
                this.tool_ch2ExportQuick.Enabled = false;
                this.tool_ch2ExportSlow.Enabled = false;
            }
        }

        private void Menu_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Tool_help_Click(object sender, EventArgs e)
        {
            Helper helper = new Helper();
            helper.Show();
        }

        private void ExportGridData(RadGridView radGridView,ToolStripComboBox cbFormat)
        {
            //if (this.radDock1.ActiveWindow == this.documentChannel1)
            //{
            //    if (this.grid_stentCompleteSignalCh1.RowCount < 1)
            //    {
            //        MessageBox.Show("没有可以导出的数据!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return;
            //    }
            //}
            //else if (this.radDock1.ActiveWindow == this.documentChannel2)
            //{
            //    if (this.grid_stentCompleteSignalCh2.RowCount < 1)
            //    {
            //        MessageBox.Show("没有可以导出的数据!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return;
            //    }
            //}
            //GridViewExport.ExportFormat currentExportType = GridViewExport.ExportFormat.EXCEL;
            //if (cbFormat.SelectedItem == null)
            //{
            //    MessageBox.Show("请选择导出格式!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            //Enum.TryParse(cbFormat.SelectedItem.ToString(), out currentExportType);
            //GridViewExport.ExportGridViewData(currentExportType, radGridView);
        }

        #region connect server
        private void ConnectServerView()
        {
            AddConnection addConnection = new AddConnection(this.serverIP, this.serverPort);
            if (addConnection.ShowDialog() == DialogResult.OK)
            {
                if (!SuperEasyClient.client.IsConnected)
                {
                    MessageBox.Show("连接服务失败！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

            }
        }
        #endregion
        private void SuperEasyClient_NoticeConnectEvent(bool IsConnect)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (IsConnect)
                {
                    this.tool_connectServer.Enabled = false;
                    this.tool_disconnect.Enabled = true;
                    this.tool_channel1Send.Enabled = true;
                    this.tool_channel1stop.Enabled = false;
                    this.tool_channel2Send.Enabled = true;
                    this.tool_channel2Stop.Enabled = false;

                    this.serverIP = AddConnection.serverIP;
                    this.serverPort = AddConnection.serverPort;

                    //RefreshGridData();
                }
                else
                {
                    this.tool_connectServer.Enabled = true;
                    this.tool_disconnect.Enabled = false;
                    this.tool_channel1Send.Enabled = false;
                    this.tool_channel1stop.Enabled = false;
                    this.tool_channel2Send.Enabled = false;
                    this.tool_channel2Stop.Enabled = false;
                    channelDataCh1.IsFirstReceive = true;
                    channelDataCh2.IsFirstReceive = true;
                }
            }));
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveStentConfig();
        }

        private void SetAutoSendSet()
        {
            Task.Run(()=>
            {
                this.BeginInvoke(new Action(() =>
                {
                    int autoSendTimeInternal = 0;
                    if (this.radDock1.ActiveWindow == this.documentChannel1)
                    {
                        autoSendTimeInternal = channelDataCh1.AutoSendTimeInternal;
                    }
                    else if (this.radDock1.ActiveWindow == this.documentChannel2)
                    {
                        autoSendTimeInternal = channelDataCh2.AutoSendTimeInternal;
                    }
                    SendSet sendSet = new SendSet(autoSendTimeInternal);
                    if (sendSet.ShowDialog() == DialogResult.OK)
                    {
                        if (this.radDock1.ActiveWindow == this.documentChannel1)
                        {
                            channelDataCh1.AutoSendTimeInternal = SendSet.autoSendTimerInternal;
                        }
                        else if (this.radDock1.ActiveWindow == this.documentChannel2)
                        {
                            channelDataCh2.AutoSendTimeInternal = SendSet.autoSendTimerInternal;
                        }
                    }
                }));
            });
        }

        private void Tool_cacheFrameAmount_Click(object sender, EventArgs e)
        {
            Task.Run(()=>
            {
                this.Invoke(new Action(() =>
                {
                    var currentFrameCh1 = this.grid_stentCompleteSignalCh1.Items.Count;
                    var currentFrameCh2 = this.grid_stentCompleteSignalCh2.Items.Count;
                    var currentFrame = 0;
                    if (currentFrameCh1 > currentFrameCh2)
                        currentFrame = currentFrameCh1;
                    else
                        currentFrame = currentFrameCh2;
                    FrameShow frameShow = new FrameShow(cacheFrameNumber, currentFrame);
                    if (frameShow.ShowDialog() == DialogResult.OK)
                    {
                        cacheFrameNumber = FrameShow.frameNumber;
                    }
                }));
            });
        }

        private void Rb_highBefore2Ch2_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignalCh2();
        }

        private void Rb_highBefore1Ch2_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignalCh2();
        }

        private void Rb_highBefore_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignalCh1();
        }

        private void Rb_highBefore2_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignalCh1();
        }

        private void Tool_pause_Click(object sender, EventArgs e)
        {
            //暂停显示
        }

        private void Tool_continue_Click(object sender, EventArgs e)
        {
            //继续显示
        }

        private void Tool_connectServer_Click(object sender, EventArgs e)
        {
            ConnectServerView();
        }

        private void Tool_disconnect_Click(object sender, EventArgs e)
        {
            DisConnectServer();
        }

        private void Menu_disconnect_Click(object sender, EventArgs e)
        {
            DisConnectServer();
        }

        private void DisConnectServer()
        {
            if (SuperEasyClient.client == null)
                return;
            SuperEasyClient.client.Close();
            if (!SuperEasyClient.client.IsConnected)
            {
                //已断开连接
                this.tool_disconnect.Enabled = false;
                this.tool_connectServer.Enabled = true;
                //this.tool_continue.Enabled = false;
                //this.tool_pause.Enabled = false;
                this.channelDataCh1.ReceivePackageInfoQueue.Clear();
                this.channelDataCh2.ReceivePackageInfoQueue.Clear();
            }
        }

        private void SuperEasyClient_NoticeMessageEvent(MyPackageInfo packageInfo)
        {
            var header = BitConverter.ToInt16(packageInfo.Header, 0);
            var headerCh1 = (int)StentSignalEnum.RequestDataCh1;
            var headerCh2 = (int)StentSignalEnum.RequestDataCh2;
            if (header == headerCh1)
            {
                channelDataCh1.ReceivePackageInfoQueue.Enqueue(packageInfo);
            }
            else if (header == headerCh2)
            {
                channelDataCh2.ReceivePackageInfoQueue.Enqueue(packageInfo);
            }
            RefreshGridData();
        }

        private void Menu_connectServer_Click(object sender, EventArgs e)
        {
            ConnectServerView();
        }

        private void RefreshGridData()
        {
            try
            {
                if (channelDataCh1.PackageInfoQueueTemp.Count <= 0)
                {
                    if (channelDataCh1.ReceivePackageInfoQueue.Count > 0)
                    {
                        channelDataCh1.PackageInfoQueueTemp.Enqueue(channelDataCh1.ReceivePackageInfoQueue.Dequeue());
                        if (channelDataCh1.IsFirstReceive)
                        {
                            channelDataCh1.IsFirstReceive = !channelDataCh1.IsFirstReceive;
                            if (channelDataCh1.PackageInfoQueueTemp.Count > 0)
                                AnalysisUsualSignalCh1(channelDataCh1.PackageInfoQueueTemp.Dequeue());
                        }
                        else
                        {
                            //等待任务完成执行
                            if (channelDataCh1.IsAnalysisComplete)
                            {
                                channelDataCh1.IsAnalysisComplete = !channelDataCh1.IsAnalysisComplete;
                                if (channelDataCh1.PackageInfoQueueTemp.Count > 0)
                                    AnalysisUsualSignalCh1(channelDataCh1.PackageInfoQueueTemp.Dequeue());
                            }
                        }
                    }
                }
                if (channelDataCh2.PackageInfoQueueTemp.Count <= 0)
                {
                    if (channelDataCh2.ReceivePackageInfoQueue.Count > 0)
                    {
                        channelDataCh2.PackageInfoQueueTemp.Enqueue(channelDataCh2.ReceivePackageInfoQueue.Dequeue());
                        if (channelDataCh2.IsFirstReceive)
                        {
                            channelDataCh2.IsFirstReceive = !channelDataCh2.IsFirstReceive;
                            if (channelDataCh2.PackageInfoQueueTemp.Count > 0)
                                AnalysisUsualSignalCh2(channelDataCh2.PackageInfoQueueTemp.Dequeue());
                        }
                        else
                        {
                            //等待任务完成执行
                            if (channelDataCh2.IsAnalysisComplete)
                            {
                                channelDataCh2.IsAnalysisComplete = !channelDataCh2.IsAnalysisComplete;
                                if (channelDataCh2.PackageInfoQueueTemp.Count > 0)
                                    AnalysisUsualSignalCh2(channelDataCh2.PackageInfoQueueTemp.Dequeue());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error(ex.Message+ex.StackTrace);
            }
        }

        private async Task<bool> AnalysisUsualSignalCh1(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length / 8;
            if (count == 0)
                return false;//长度不足
            await Task.Run(() =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    int j = 0;
                    for (int i = 0; i < count * 8; i += 8)
                    {
                        channelDataCh1.ChannelType = ChannelData.ChannelTypeEnum.Channel1;
                        var iData = AnalysisSlowSignalData(packageInfo, i, channelDataCh1,count,j);
                        CacheListViewUpdateSinalCh1(packageInfo,i,count,j);
                        //Application.DoEvents();
                        if(channelDataCh1.RevCount < cacheFrameNumber)
                            channelDataCh1.RevCount++;
                        j++;
                    }
                }));
            });
            channelDataCh1.IsAnalysisComplete = true;
            return true;
        }

        private async Task<bool> AnalysisUsualSignalCh2(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length / 8;
            if (count == 0)
                return false;//长度不足
            await Task.Run(() =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    int j = 0;
                    for (int i = 0; i < count * 8; i += 8)
                    {
                        channelDataCh2.ChannelType = ChannelData.ChannelTypeEnum.Channel2;
                        var iData = AnalysisSlowSignalData(packageInfo, i, channelDataCh2,count,j);
                        CacheListViewUpdateSinalCh2(packageInfo, i, count, j);
                        Application.DoEvents();
                        if (channelDataCh2.RevCount < this.cacheFrameNumber)
                            channelDataCh2.RevCount++;
                        j++;
                    }
                }));
            });
            channelDataCh2.IsAnalysisComplete = true;
            return true;
        }

        private void CacheListViewUpdateSinalCh1(MyPackageInfo packageInfo, int i,int countPerPackage,int j)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                    ListViewItem listViewItem = new ListViewItem();
                    List<string> list = new List<string>();
                    //listViewItem.Text = (channelDataCh1.RevCount + 1).ToString();
                    listViewItem.Text = packageInfo.Data[i].ToString("X2");
                    list.Add(packageInfo.Data[i + 1].ToString("X2"));
                    list.Add(packageInfo.Data[i + 2].ToString("X2"));
                    list.Add(packageInfo.Data[i + 3].ToString("X2"));
                    list.Add(packageInfo.Data[i + 4].ToString("X2"));
                    list.Add(packageInfo.Data[i + 5].ToString("X2"));
                    list.Add(packageInfo.Data[i + 6].ToString("X2"));
                    list.Add(packageInfo.Data[i + 7].ToString("X2"));
                    list.Add(Crc4_Cal(crcList));
                    listViewItem.SubItems.AddRange(list.ToArray());

                    cacheListPerpTempCompleteCh1.Add(listViewItem);
                    if (cacheCountCh1 == 100)
                    {
                        ReSetCompleteCh1(cacheListPerpTempCompleteCh1);
                        cacheCountCh1 = 0;
                        AnalysisQuickSignalCh1();
                    }
                    else if (channelDataCh1.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - cacheActualCountCompleteCh1) == (countPerPackage % 100))
                    {
                        if (cacheCountCh1 == (countPerPackage % 100))
                        {
                            ReSetCompleteCh1(cacheListPerpTempCompleteCh1);
                            cacheCountCh1 = 0;
                            cacheActualCountCompleteCh1 = 0;
                            AnalysisQuickSignalCh1();
                        }
                    }
                    cacheCountCh1++;
                }
                catch (Exception ex)
                {
                    LogHelper.Log.Error("updatech1complete " + ex.Message + ex.StackTrace);
                }
            }));
        }

        private void CacheListViewUpdateSinalCh2(MyPackageInfo packageInfo, int i, int countPerPackage, int j)
        {
            this.Invoke(new Action(() =>
            {
                int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                ListViewItem listViewItem = new ListViewItem();
                List<string> list = new List<string>();
                //listViewItem.Text = (channelDataCh2.RevCount + 1).ToString();
                listViewItem.Text = packageInfo.Data[i].ToString("X2");
                list.Add(packageInfo.Data[i + 1].ToString("X2"));
                list.Add(packageInfo.Data[i + 2].ToString("X2"));
                list.Add(packageInfo.Data[i + 3].ToString("X2"));
                list.Add(packageInfo.Data[i + 4].ToString("X2"));
                list.Add(packageInfo.Data[i + 5].ToString("X2"));
                list.Add(packageInfo.Data[i + 6].ToString("X2"));
                list.Add(packageInfo.Data[i + 7].ToString("X2"));
                list.Add(Crc4_Cal(crcList));
                listViewItem.SubItems.AddRange(list.ToArray());

                cacheListPerpTempCompleteCh2.Add(listViewItem);
                if (cacheCountCh2 == 100)
                {
                    ReSetCompleteCh2(cacheListPerpTempCompleteCh2);
                    this.grid_stentCompleteSignalCh2.Items[this.grid_stentCompleteSignalCh2.Items.Count - 1].EnsureVisible();
                    cacheCountCh2 = 0;
                    AnalysisQuickSignalCh2();
                }
                else if (channelDataCh2.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - cacheActualCountCompleteCh2) == (countPerPackage % 100))
                {
                    if (cacheCountCh2 == (countPerPackage % 100))
                    {
                        ReSetCompleteCh2(cacheListPerpTempCompleteCh2);
                        this.grid_stentCompleteSignalCh2.Items[this.grid_stentCompleteSignalCh2.Items.Count - 1].EnsureVisible();
                        cacheCountCh2 = 0;
                        cacheActualCountCompleteCh2 = 0;
                        AnalysisQuickSignalCh2();
                    }
                }
                cacheCountCh2++;
            }));
        }

        private string AnalysisSlowSignalData(MyPackageInfo packageInfo,int index,ChannelData channelData,int countPerPackage,int indexPerPackage)
        {
            if (indexPerPackage == 0)
            {
                //channelData.CacheFirstBitValue.Clear();
                //channelData.CacheDataPerFrame.Clear();
            }
            var iData = packageInfo.Data[index].ToString("X2") + "-" + packageInfo.Data[index + 1].ToString("X2") + "-" + packageInfo.Data[index + 2].ToString("X2") + "-" + packageInfo.Data[index + 3].ToString("X2") + "-" + packageInfo.Data[index + 4].ToString("X2") + "-" + packageInfo.Data[index + 5].ToString("X2") + "-" + packageInfo.Data[index + 6].ToString("X2") + "-" + packageInfo.Data[index + 7].ToString("X2");
            var firstByteString = Convert.ToString(packageInfo.Data[index], 2).PadLeft(4, '0');
            var firstBit = firstByteString.Substring(firstByteString.Length - 4, 1);//从右往左数，起始位为0，第3位
            channelData.CacheFirstBitValue.Add(int.Parse(firstBit));
            channelData.CacheDataPerFrame.Add(iData);
            //当小于16帧或18帧时，如果既不满足标准帧，也不满足扩展帧，全部缓存清零
            if (channelData.CacheFirstBitValue.Count <= 18)
            {
                if (!IsStentStandardFrameType(channelData.CacheFirstBitValue) && !IsStentExtentFrameType(channelData.CacheFirstBitValue))
                {
                    ClearCacheSignal(channelData);
                    return iData;//不足一个完整包，return
                }
            }
            //当大于16帧时，判断是否是标准帧，开始向下处理
            //开始判断数据类型：标准帧类/扩展帧类型

            if (channelData.CacheFirstBitValue.Count >= 18 && IsStentExtentFrameType(channelData.CacheFirstBitValue))
            {
                //扩展帧为18帧为一完整包
                //起始位为0的第二位与第三位
                var messageID = "";
                var data = "";
                var dataMessageID = "";//message id剩余部分
                int count = 0;
                var sumCRC = "";
                var crcValue = "";
                foreach (var signalData in channelData.CacheDataPerFrame)
                {
                    var bitData = Convert.ToString(Convert.ToByte(signalData.Substring(0, 2), 16), 2).PadLeft(4, '0');
                    if (channelData.CacheFirstBitValue[7] == 0)
                    {
                        //this is 12-bit-data and 8-bit-message-id
                        //index:6-17
                        if (count >= 6 && count <= 17)
                        {
                            data += bitData.Substring(bitData.Length - 3, 1);
                            if (count >= 8 && count <= 11)
                            {
                                messageID += bitData.Substring(bitData.Length - 4, 1);
                            }
                            else if (count >= 13 && count <= 16)
                            {
                                messageID += bitData.Substring(bitData.Length - 4, 1);
                            }
                        }
                    }
                    else if (channelData.CacheFirstBitValue[7] == 1)
                    {
                        //this is 16-bit-data and 4-bit-message-id
                        if (count >= 6 && count <= 17)
                        {
                            data += bitData.Substring(bitData.Length - 3, 1);
                            if (count >= 8 && count <= 11)
                            {
                                messageID += bitData.Substring(bitData.Length - 4, 1);
                            }
                            else if (count >= 13 && count <= 16)
                            {
                                dataMessageID += bitData.Substring(bitData.Length - 4, 1);
                            }
                        }
                    }
                    //计算CRC
                    if (count >= 0 && count <= 5)
                    {
                        crcValue += bitData.Substring(bitData.Length - 3, 1);
                    }
                    if (count >= 6 && count <= 17)
                    {
                        sumCRC += bitData.Substring(bitData.Length - 3, 1) + bitData.Substring(bitData.Length - 4, 1);
                    }
                    count++;
                }
                dataMessageID += data;
                data = "0X" + string.Format("{0:x2}", Convert.ToInt32(data, 2));
                messageID = "0X" + string.Format("{0:x2}", Convert.ToInt32(messageID, 2));
                crcValue = string.Format("{0:X4}", Convert.ToInt32(crcValue, 2));
                var sumCRCCal = Crc6_Cal(Add6Bit2Array(sumCRC));
                if (crcValue == sumCRCCal)
                {
                    //LogHelper.Log.Info("【扩展帧】CRC校验成功 " + sumCRCCal);
                    //一包数据解析完成
                    //开始显示一包数据
                    channelData.FrameType = ChannelData.FrameTypeEnum.ExtendFrame;
                    UpdateSlowSignalGridData(channelData, messageID, data, crcValue, sumCRCCal, countPerPackage / 18);
                }
                else
                {
                    LogHelper.Log.Info($"【扩展帧】CRC校验失败 sumCRCCal={sumCRCCal} crcValue={crcValue}  ");
                }
                //清空缓存
                channelData.CacheDataPerFrame.Clear();
                channelData.CacheFirstBitValue.Clear();
            }
            else if (channelData.CacheFirstBitValue.Count >= 16 && IsStentStandardFrameType(channelData.CacheFirstBitValue))
            {
                //标准帧为16帧为一个完整包
                //获取message ID 与data  起始位为0的第2位
                var messageID = "";
                var data = "";
                int count = 0;
                var sumCRC = "";
                var sumCRCValue = "";
                var crcValue = "";
                foreach (var signalData in channelData.CacheDataPerFrame)
                {
                    var bitData = Convert.ToString(Convert.ToByte(signalData.Substring(0, 2), 16), 2).PadLeft(4, '0');//4位bit
                    if (count <= 3)
                    {
                        messageID += bitData.Substring(bitData.Length - 3, 1);
                    }
                    if (count >= 4 && count <= 11)
                    {
                        data += bitData.Substring(bitData.Length - 3, 1);
                    }
                    if (count >= 0 && count <= 11)
                    {
                        sumCRC += bitData.Substring(bitData.Length - 3, 1);
                    }
                    if (count >= 12 && count <= 15)
                    {
                        crcValue += bitData.Substring(bitData.Length - 3, 1);
                    }
                    count++;
                }
                var data1 = data.Substring(0, 4);
                var data2 = data.Substring(4, 4);

                data = "0X" + string.Format("{0:x2}", Convert.ToInt32(data, 2));
                int[] crcCheckArray = new int[] { Convert.ToInt32(messageID, 2), Convert.ToInt32(data1, 2), Convert.ToInt32(data2, 2) };
                messageID = "0X" + string.Format("{0:x2}", Convert.ToInt32(messageID, 2));
                //sumCRC = string.Format("{0:X4}",Convert.ToInt32(sumCRC, 2));

                sumCRCValue = Crc4_Cal(crcCheckArray);
                crcValue = string.Format("{0:X4}", Convert.ToInt32(crcValue, 2));
                if (sumCRCValue == crcValue)
                {
                    //LogHelper.Log.Info("【标准帧】校验成功 " + sumCRC);
                    //一包数据解析完成
                    //开始显示一包数据
                    channelData.FrameType = ChannelData.FrameTypeEnum.StandardFrame;
                    UpdateSlowSignalGridData(channelData, messageID, data, crcValue, sumCRCValue, countPerPackage / 16);
                }
                else
                {
                    LogHelper.Log.Info($"【标准帧】校验失败 crcValue={crcValue} sumCRCValue={sumCRCValue}");
                }
                //清空缓存
                channelData.CacheDataPerFrame.Clear();
                channelData.CacheFirstBitValue.Clear();
            }

            //二进制位iData = Convert.ToString(packageInfo.Data[index], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 1], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 2], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 3], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 4], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 5], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 6], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 7], 2).PadLeft(4, '0');
            return iData;
        }

        private void UpdateSlowSignalGridData(ChannelData channelData,string messageID,string data,string crcValue,string sumCRCCal,int countPerPackage)
        {
            try
            {
                if (channelData.ChannelType == ChannelData.ChannelTypeEnum.Channel1)
                {
                    this.Invoke(new Action(() =>
                    {
                        ListViewItem listViewItem = new ListViewItem();
                        List<string> list = new List<string>();
                        //listViewItem.Text = (channelData.SlowSignalCount + 1).ToString();
                        if (channelData.FrameType == ChannelData.FrameTypeEnum.StandardFrame)
                        {
                            listViewItem.Text = "标准帧";
                        }
                        else if (channelData.FrameType == ChannelData.FrameTypeEnum.ExtendFrame)
                        {
                            //list.Add("扩展帧");
                            listViewItem.Text = "扩展帧";
                        }
                        list.Add(messageID);
                        list.Add(data);
                        //list.Add(crcValue);
                        listViewItem.SubItems.AddRange(list.ToArray());
                        this.cacheListPerpTempSlowCh1.Add(listViewItem);

                        if (cacheCountCh3 == 20)
                        {
                            ReSetSlowCh1(cacheListPerpTempSlowCh1);
                            cacheCountCh3 = 0;
                        }
                        else if (channelDataCh1.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - cacheActualCountSlowCh1) == (countPerPackage % 20))
                        {
                            if ((cacheCountCh3 == (countPerPackage % 20)))
                            {
                                ReSetSlowCh1(cacheListPerpTempSlowCh1);
                                cacheCountCh3 = 0;
                                cacheActualCountSlowCh1 = 0;
                            }
                        }
                        cacheCountCh3++;
                        if (channelData.SlowSignalCount < this.cacheFrameNumber)
                            channelData.SlowSignalCount++;
                    }));
                }
                else if (channelData.ChannelType == ChannelData.ChannelTypeEnum.Channel2)
                {
                    this.Invoke(new Action(() =>
                    {
                        ListViewItem listViewItem = new ListViewItem();
                        List<string> list = new List<string>();
                        //listViewItem.Text = (channelData.SlowSignalCount + 1).ToString();
                        if (channelData.FrameType == ChannelData.FrameTypeEnum.StandardFrame)
                        {
                            //list.Add("标准帧");
                            listViewItem.Text = "标准帧";
                        }
                        else if (channelData.FrameType == ChannelData.FrameTypeEnum.ExtendFrame)
                        {
                            //list.Add("扩展帧");
                            listViewItem.Text = "扩展帧";
                        }
                        list.Add(messageID);
                        list.Add(data);
                        //list.Add(crcValue);
                        //list.Add(sumCRCCal);
                        listViewItem.SubItems.AddRange(list.ToArray());
                        cacheListPerpTempSlowCh2.Add(listViewItem);

                        if (cacheCountCh4 == 20)
                        {
                            ReSetSlowCh2(cacheListPerpTempSlowCh2);
                            this.grid_stentSlowSignalCh2.Items[this.grid_stentSlowSignalCh2.Items.Count - 1].EnsureVisible();
                            cacheCountCh4 = 0;
                        }
                        else if (channelDataCh2.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - cacheActualCountSlowCh2) == (countPerPackage % 20))
                        {
                            if (cacheCountCh4 == (countPerPackage % 20))
                            {
                                ReSetSlowCh2(cacheListPerpTempSlowCh2);
                                this.grid_stentSlowSignalCh2.Items[this.grid_stentSlowSignalCh2.Items.Count - 1].EnsureVisible();
                                cacheCountCh4 = 0;
                                cacheActualCountSlowCh2 = 0;
                            }
                        }
                        cacheCountCh4++;
                        if (channelData.SlowSignalCount < this.cacheFrameNumber)
                            channelData.SlowSignalCount++;
                    }));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("updatech1slow " + ex.Message + ex.StackTrace);
            }
        }

        private void ClearCacheSignal(ChannelData channelData)
        {
            var lastFrame = channelData.CacheDataPerFrame[channelData.CacheDataPerFrame.Count - 1];
            if (channelData.CacheFirstBitValue[channelData.CacheFirstBitValue.Count - 1] == 1)
            {
                channelData.CacheFirstBitValue.Clear();
                channelData.CacheDataPerFrame.Clear();
                channelData.CacheFirstBitValue.Add(1);
                channelData.CacheDataPerFrame.Add(lastFrame);
            }
            else
            {
                channelData.CacheFirstBitValue.Clear();
                channelData.CacheDataPerFrame.Clear();
            }
        }

        #region quick signal 
        /*
         * 【快信号】
         * 1）显示最新数据
         * 2）根据高低位顺序，重新计算数据
         */
        private void AnalysisQuickSignalCh1()
        {
            //将显示数据最新一条数据，按高低位排序显示
            if (this.grid_stentCompleteSignalCh1.Items.Count < 1)
                return;
            //if (this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[2].Value == null)
            //    return;
            ListViewItem listViewItem = this.grid_stentCompleteSignalCh1.Items[this.grid_stentCompleteSignalCh1.Items.Count - 1];
            var latestValue = listViewItem.SubItems[1].Text.ToString() + listViewItem.SubItems[2].Text + listViewItem.SubItems[3].Text + listViewItem.SubItems[4].Text + listViewItem.SubItems[5].Text + listViewItem.SubItems[6].Text;
            byte[] latestByte = ConvertByte.HexToByte(latestValue);
            byte[] data1 = new byte[3];
            byte[] data2 = new byte[3];
            Array.Copy(latestByte,0,data1,0,3);
            Array.Copy(latestByte,3,data2,0,3);
            //数据1
            if (rb_highBefore1Ch1.CheckState == CheckState.Checked)
            {

            }
            else if(rb_lowerBefore1Ch1.CheckState == CheckState.Checked)
            {
                data1 = data1.Reverse().ToArray();
            }
            //数据2
            if (this.rb_highBefore2Ch1.CheckState == CheckState.Checked)
            {
            }
            else if (this.rb_lowerBefore2Ch1.CheckState == CheckState.Checked)
            {
                data2 = data2.Reverse().ToArray();
            }
            this.grid_stentQuickBothCh1.Invoke(new Action(()=>
            {
                //显示数据1与数据2
                this.grid_stentQuickBothCh1.Items.Clear();
                ListViewItem lvItem = new ListViewItem();
                List<string> list = new List<string>();
                var d1 = BitConverter.ToString(data1).Replace("-", "");
                var d2 = BitConverter.ToString(data2).Replace("-", "");
                d1 = d1.Substring(1, 1) + d1.Substring(3, 1) + d1.Substring(5, 1);
                d2 = d2.Substring(1, 1) + d2.Substring(3, 1) + d2.Substring(5, 1);
                lvItem.Text = d1;
                list.Add(d2);
                lvItem.SubItems.AddRange(list.ToArray());
                this.grid_stentQuickBothCh1.Items.Add(lvItem);
            }));
        }

        private void AnalysisQuickSignalCh2()
        {
            //将显示数据最新一条数据，按高低位排序显示
            if (this.grid_stentCompleteSignalCh2.Items.Count < 1)
                return;
            ListViewItem listViewItem = this.grid_stentCompleteSignalCh2.Items[this.grid_stentCompleteSignalCh2.Items.Count - 1];
            var latestValue = listViewItem.SubItems[1].Text.ToString() + listViewItem.SubItems[2].Text + listViewItem.SubItems[3].Text + listViewItem.SubItems[4].Text + listViewItem.SubItems[5].Text + listViewItem.SubItems[6].Text;
            byte[] latestByte = ConvertByte.HexToByte(latestValue);
            byte[] data1 = new byte[3];
            byte[] data2 = new byte[3];
            Array.Copy(latestByte, 0, data1, 0, 3);
            Array.Copy(latestByte, 3, data2, 0, 3);
            //数据1
            if (rb_highBefore1Ch2.CheckState == CheckState.Checked)
            {

            }
            else if (rb_lowerBefore1Ch2.CheckState == CheckState.Checked)
            {
                data1 = data1.Reverse().ToArray();
            }
            //数据2
            if (this.rb_highBefore2Ch2.CheckState == CheckState.Checked)
            {
            }
            else if (this.rb_lowerBefore2Ch2.CheckState == CheckState.Checked)
            {
                data2 = data2.Reverse().ToArray();
            }
            this.grid_stentQuickBothCh2.Invoke(new Action(() =>
            {
                //显示数据1与数据2
                //显示数据1与数据2
                this.grid_stentQuickBothCh2.Items.Clear();
                ListViewItem lvItem = new ListViewItem();
                List<string> list = new List<string>();
                var d1 = BitConverter.ToString(data1).Replace("-", "");
                var d2 = BitConverter.ToString(data2).Replace("-", "");
                d1 = d1.Substring(1,1) + d1.Substring(3,1) + d1.Substring(5,1);
                d2 = d2.Substring(1,1) + d2.Substring(3,1) + d2.Substring(5,1);
                lvItem.Text = d1;
                list.Add(d2);
                lvItem.SubItems.AddRange(list.ToArray());
                this.grid_stentQuickBothCh2.Items.Add(lvItem);
            }));
        }

        #endregion

        #region stent signal cal function

        private bool IsStentStandardFrameType(List<int> result)
        {
            int[] standFrameList = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //判断标准帧
            int i = 0;
            foreach (var frame in result)
            {
                if (frame != standFrameList[i])
                    return false;
                i++;
            }
            return true;
        }

        private bool IsStentExtentFrameType(List<int> result)
        {
            int[] extendFrameList = new int[] { 1, 1, 1, 1, 1, 1, 0, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, 0 };
            //判断扩展帧
            //7/13/18帧对应值为0，1-6帧对应值为1
            int i = 0;
            foreach (var frame in result)
            {
                if ((i >= 0 && i <= 5) || i == 6 || i == 12 || i == 17)
                {
                    if (frame != extendFrameList[i])
                        return false;
                }
                i++;
            }
            return true;
        }

        private int[] Add6Bit2Array(string crcSum)
        {
            if (crcSum.Length != 24)
                return new int[] { };
            int[] crcArray = new int[4];
            crcArray[0] = Convert.ToInt32(crcSum.Substring(0, 6), 2);
            crcArray[1] = Convert.ToInt32(crcSum.Substring(6, 6), 2);
            crcArray[2] = Convert.ToInt32(crcSum.Substring(12, 6), 2);
            crcArray[3] = Convert.ToInt32(crcSum.Substring(18, 6), 2);
            return crcArray;
        }

        private int[] Add4Bit2Array(string crcSum)
        {
            if (crcSum.Length != 12)
                return new int[] { };
            int[] crcArray = new int[3];
            crcArray[0] = Convert.ToInt32(crcSum.Substring(0, 4), 2);
            crcArray[1] = Convert.ToInt32(crcSum.Substring(4, 4), 2);
            crcArray[2] = Convert.ToInt32(crcSum.Substring(8, 4), 2);
            return crcArray;
        }

        /// <summary>
        /// CRC6计算
        /// </summary>
        /// <param name="dataResult">取24个bit位，分割成6个为一组</param>
        /// <returns></returns>
        private string Crc6_Cal(int[] dataResult)/*data位6位nibble块的值，len为数据块的数量*/
        {
            if (dataResult.Length == 0)
                return "";
            int[] crcCalList = new int[] { 0, 25, 50, 43, 61, 36, 15, 22, 35, 58, 17, 8, 30, 7, 44 ,53,
             31, 6, 45, 52, 34, 59, 16, 9, 60, 37, 14, 23, 1, 24, 51, 42,
             62, 39, 12, 21, 3, 26, 49, 40, 29, 4, 47, 54, 32, 57, 18, 11,
             33, 56, 19, 10, 28, 5, 46, 55, 2, 27, 48, 41, 63, 38, 13, 20};
            /*crc初始值*/
            var result = 0x15;
            /*查表地址*/
            var tableNo = 0;

            /*对额外添加的6个0进行查表计算crc*/
            tableNo = result ^ 0;
            result = crcCalList[tableNo];

            /*对数组数据查表计算crc*/
            for (int i = 0; i < dataResult.Length; i++)
            {
                tableNo = result ^ dataResult[i];
                result = crcCalList[tableNo];
            }

            /*返回最终的crc值*/
            return string.Format("{0:X4}",result);
        }

        /// <summary>
        /// CRC4计算
        /// </summary>
        /// <param name="dataResult">D1-D6的具体值</param>
        /// <returns></returns>
        private string Crc4_Cal(int[] dataResult)/*data位4位nibble块的值，len为nibble块的数量*/
        {
            if (dataResult.Length == 0)
                return "";
            int[] crcCalList = new int[]{ 0, 13, 7, 10, 14, 3, 9, 4, 1, 12, 6, 11, 15, 2, 8, 5 };
            var result = 0x03;
            var tableNo = 0;
            for (int i = 0; i < dataResult.Length; i++)
            {
                tableNo = result ^ dataResult[i];
                result = crcCalList[tableNo];
            }
            return string.Format("{0:X4}", result);
        }
        #endregion

        #region init 
        private void Init()
        {
            this.rb_highBefore1Ch1.CheckState = CheckState.Checked;
            this.rb_highBefore2Ch1.CheckState = CheckState.Checked;
            this.rb_highBefore1Ch2.CheckState = CheckState.Checked;
            this.rb_highBefore2Ch2.CheckState = CheckState.Checked;

            //this.tool_continue.Enabled = false;
            //this.tool_pause.Enabled = false;
            this.tool_channel1Send.Enabled = false;
            this.tool_channel2Send.Enabled = false;
            this.tool_channel1stop.Enabled = false;
            this.tool_channel2Stop.Enabled = false;

            channelDataCh1 = new ChannelData();
            channelDataCh2 = new ChannelData();
            channelDataCh1.CacheFirstBitValue = new List<int>();
            channelDataCh1.CacheDataPerFrame = new List<string>();
            channelDataCh2.CacheFirstBitValue = new List<int>();
            channelDataCh2.CacheDataPerFrame = new List<string>();
            channelDataCh1.ReceivePackageInfoQueue = new Queue<MyPackageInfo>();
            channelDataCh1.PackageInfoQueueTemp = new Queue<MyPackageInfo>();
            channelDataCh2.ReceivePackageInfoQueue = new Queue<MyPackageInfo>();
            channelDataCh2.PackageInfoQueueTemp = new Queue<MyPackageInfo>();
            //config
            stentConfigDirectory = AppDomain.CurrentDomain.BaseDirectory + "config\\";
            if (!Directory.Exists(stentConfigDirectory))
                Directory.CreateDirectory(stentConfigDirectory);
            ReadConfig();
        }
        #endregion

        #region ini config params
        private void ReadConfig()
        {
            var stentConfigPath = stentConfigDirectory + STENT_CONFIG_FILE;
            if (!File.Exists(stentConfigPath))
                return;
            var frameCount = INIFile.GetValue(STENT_CONFIG_SECTION,STENT_CONFIG_FRAME_COUNT_KEY, stentConfigPath);
            if (frameCount != "")
                int.TryParse(frameCount,out cacheFrameNumber);
            if (cacheFrameNumber == 0)
                cacheFrameNumber = 10000;

            //ch1 timeInternal
            int autoSendTimeInternal = 7000;
            var timeInternal = INIFile.GetValue(STENT_CONFIG_SECTION_CH1,STENT_CONFIG_TIME_INTERNAL,stentConfigPath);
            if (timeInternal != "")
                int.TryParse(timeInternal,out autoSendTimeInternal);
            if (autoSendTimeInternal == 0)
                autoSendTimeInternal = 7000;
            channelDataCh1.AutoSendTimeInternal = autoSendTimeInternal;

            //ch2 timeInternal
            timeInternal = INIFile.GetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_TIME_INTERNAL, stentConfigPath);
            if (timeInternal != "")
                int.TryParse(timeInternal, out autoSendTimeInternal);
            if (autoSendTimeInternal == 0)
                autoSendTimeInternal = 7000;
            channelDataCh2.AutoSendTimeInternal = autoSendTimeInternal;

            //ch1 is auto send 
            bool IsAutoSend = false;
            var isAuto = INIFile.GetValue(STENT_CONFIG_SECTION_CH1,STENT_CONFIG_IS_AUTO_KEY,stentConfigPath);
            if (isAuto != "")
                bool.TryParse(isAuto,out IsAutoSend);

            //ch2 is auto send
            isAuto = INIFile.GetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_IS_AUTO_KEY, stentConfigPath);
            if (isAuto != "")
                bool.TryParse(isAuto, out IsAutoSend);

            serverIP = INIFile.GetValue(STENT_CONFIG_SECTION, STENT_CONFIG_SERVER_URL, stentConfigPath);
            var port = INIFile.GetValue(STENT_CONFIG_SECTION, STENT_CONFIG_SERVER_PORT, stentConfigPath);
            if (port != "")
                int.TryParse(port, out serverPort);
        }

        private void SaveStentConfig()
        {
            var stentConfigPath = stentConfigDirectory + STENT_CONFIG_FILE;
            //public
            INIFile.SetValue(STENT_CONFIG_SECTION, STENT_CONFIG_SERVER_URL, serverIP, stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION, STENT_CONFIG_SERVER_PORT, serverPort.ToString(), stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION, STENT_CONFIG_FRAME_COUNT_KEY, cacheFrameNumber.ToString(), stentConfigPath);
            //ch1
            //INIFile.SetValue(STENT_CONFIG_SECTION_CH1, STENT_CONFIG_IS_AUTO_KEY,channelDataCh1.IsAutoSend.ToString(),stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION_CH1, STENT_CONFIG_TIME_INTERNAL,channelDataCh1.AutoSendTimeInternal.ToString(),stentConfigPath);
            //ch2
            //INIFile.SetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_IS_AUTO_KEY, channelDataCh2.IsAutoSend.ToString(), stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_TIME_INTERNAL, channelDataCh2.AutoSendTimeInternal.ToString(), stentConfigPath);
        }
        #endregion

        #region sendMessage
        private void SendMessageCh1()
        {
            SuperEasyClient.SendMessage(StentSignalEnum.RequestDataCh1, new byte[0]);
        }

        private void SendMessageCh2()
        {
            SuperEasyClient.SendMessage(StentSignalEnum.RequestDataCh2, new byte[0]);
        }
        #endregion

        #region export listView data
        private void ExportListViewData(ListView listView)
        {
            if (listView.Items == null) 
                return;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = "xls";
            saveDialog.Filter = "Excel文件 | *.xls";
            saveDialog.FileName = "data_"+DateTime.Now.ToString("yyyyMMddHHmmss");
            saveDialog.ShowDialog();
            var saveFileName = saveDialog.FileName;
            if (saveFileName.IndexOf(":") < 0)
                return;
            if (File.Exists(saveFileName)) 
                File.Delete(saveFileName);
            Excel.Application xlApp = new Excel.Application();
            if (xlApp == null)
            {
                MessageBox.Show("无法创建Excel对象，可能您的机器未安装Excel");
                return;
            }
            Excel.Workbooks workbooks = xlApp.Workbooks;
            Excel.Workbook workbook = workbooks.Add(true);
            Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets[1];
            xlApp.Visible = false;
            //填充列 
            for (int i = 0; i < listView.Columns.Count; i++)
            {
                worksheet.Cells[1, i + 1] = listView.Columns[i].Text.ToString();
                ((Microsoft.Office.Interop.Excel.Range)worksheet.Cells[1, i + 1]).Font.Bold = true;
            }
            //填充数据（这里分了两种情况，1：lv带CheckedBox，2：不带CheckedBox） 
            //带CheckedBoxes 
            if (listView.CheckBoxes == true)
            {
                int tmpCnt = 0;
                for (int i = 0; i < listView.Items.Count; i++)
                {
                    if (listView.Items[i].Checked == true)
                    {
                        for (int j = 0; j < listView.Columns.Count; j++)
                        {
                            if (j == 0)
                            {
                                worksheet.Cells[2 + tmpCnt, j + 1] = listView.Items[i].Text.ToString();
                                ((Microsoft.Office.Interop.Excel.Range)worksheet.Cells[2 + tmpCnt, j + 1]).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                            }
                            else
                            {
                                worksheet.Cells[2 + tmpCnt, j + 1] = listView.Items[i].SubItems[j].Text.ToString();
                                ((Excel.Range)worksheet.Cells[2 + tmpCnt, j + 1]).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                            }
                        }
                        tmpCnt++;
                    }
                }
            }
            else //不带Checkedboxe 
            {
                for (int j = 0; j < listView.Columns.Count; j++)
                {
                    for (int i = 0; i < listView.Items.Count; i++)
                    {
                        if (j == 0)
                        {
                            worksheet.Cells[2 + i, j + 1] = listView.Items[i].Text.ToString();
                            ((Excel.Range)worksheet.Cells[2 + i, j + 1]).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                        }
                        else
                        {
                            worksheet.Cells[2 + i, j + 1] = listView.Items[i].SubItems[j].Text.ToString();
                            ((Excel.Range)worksheet.Cells[2 + i, j + 1]).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                        }
                    }
                }
            }
            object missing = System.Reflection.Missing.Value;
            try
            {
                workbook.Saved = true;
                workbook.SaveAs(saveFileName, Excel.XlFileFormat.xlXMLSpreadsheet, missing, missing, false, false, Excel.XlSaveAsAccessMode.xlNoChange, missing, missing, missing, missing, missing);
            }
            catch (Exception e1)
            {
                MessageBox.Show("导出文件时出错, 文件可能正被打开！\n" +e1.Message);
                return;
            }
            finally
            {
                xlApp.Quit();
                System.GC.Collect();
            }
            MessageBox.Show("导出Excle成功！");
        }

        private async void  DoExportListView(ListView listView,string saveFileName)
        {
            int rowNum = listView.Items.Count;
            if (rowNum == 0)
            {
                MessageBox.Show("没有可以导出的数据！","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = "xls";
            saveDialog.Filter = "Excel文件 | *.xls";
            saveDialog.FileName = saveFileName;
            saveDialog.ShowDialog();
            var strFileName = saveDialog.FileName;
            if (strFileName.IndexOf(":") < 0)
                return;
            if (string.IsNullOrEmpty(strFileName))
                return;
            if (File.Exists(strFileName))
                File.Delete(strFileName);

            int columnNum = listView.Items[0].SubItems.Count;
            int rowIndex = 1;
            int columnIndex = 0;

            await Task.Run(()=>
            {
                if (rowNum > 0)
                {
                    Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
                    if (xlApp == null)
                    {
                        MessageBox.Show("无法创建excel对象，可能您系统没有安装excel");
                        return;
                    }
                    xlApp.DefaultFilePath = "";
                    xlApp.DisplayAlerts = true;
                    xlApp.SheetsInNewWorkbook = 1;
                    Excel.Workbook xlBook = xlApp.Workbooks.Add(true);
                    //将ListView中的列名导入Excel表第一行
                    foreach (ColumnHeader dc in listView.Columns)
                    {
                        columnIndex++;
                        xlApp.Cells[rowIndex, columnIndex] = dc.Text;
                    }
                    for (int i = 0; i < rowNum; i++)
                    {
                        rowIndex++;
                        columnIndex = 0;
                        for (int j = 0; j < columnNum; j++)
                        {
                            columnIndex++;
                            //注意这个在导出的时候加了"\t"的目的是避免导出的数据显示为科学计数法，可以放在每行的首尾
                            xlApp.Cells[rowIndex, columnIndex] = Convert.ToString(listView.Items[i].SubItems[j].Text) + "\t";
                        }
                    }
                    xlBook.SaveAs(strFileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    xlApp = null;
                    xlBook.Close();
                    xlBook = null;
                }
            });
            MessageBox.Show("导出数据完成！","Info",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        #endregion

        #region temp
        private void UpdateSinalCh1(MyPackageInfo packageInfo, int i, int countPerPackage, int j)
        {
            this.Invoke(new Action(() =>
            {
                //int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                //this.grid_stentCompleteSignalCh1.BeginEdit();
                //this.grid_stentCompleteSignalCh1.Rows.AddNew();
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[0].Value = channelDataCh1.RevCount + 1;
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[1].Value = packageInfo.Data[i].ToString("X2");//status
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[2].Value = packageInfo.Data[i + 1].ToString("X2");//data1
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[3].Value = packageInfo.Data[i + 2].ToString("X2");//data2
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[4].Value = packageInfo.Data[i + 3].ToString("X2");//data3
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[5].Value = packageInfo.Data[i + 4].ToString("X2");//data4
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[6].Value = packageInfo.Data[i + 5].ToString("X2");//data5
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[7].Value = packageInfo.Data[i + 6].ToString("X2");//data6
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[8].Value = packageInfo.Data[i + 7].ToString("X2");//crc
                //this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[9].Value = Crc4_Cal(crcList);
                //this.grid_stentCompleteSignalCh1.EndEdit();
                ////清除数据到最大缓存
                //if (this.grid_stentCompleteSignalCh1.RowCount > cacheFrameNumber)
                //{
                //    this.grid_stentCompleteSignalCh1.Rows.RemoveAt(0);
                //    channelDataCh1.RevCount -= 1;
                //    int id = 1;
                //    foreach (var rowInfo in this.grid_stentCompleteSignalCh1.Rows)
                //    {
                //        rowInfo.Cells[0].Value = id;
                //        id++;
                //    }
                //}

                //if (cacheCountCh1 == 50)
                //{
                //    //this.grid_stentCompleteSignalCh1.Update();
                //    this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].IsSelected = true;
                //    this.grid_stentCompleteSignalCh1.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh1.Rows.Count);
                //    cacheCountCh1 = 0;
                //}
                //else if (channelDataCh1.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - j) <= 50)
                //{
                //    //this.grid_stentCompleteSignalCh1.Update();
                //    this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].IsSelected = true;
                //    this.grid_stentCompleteSignalCh1.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh1.Rows.Count);
                //}
                cacheCountCh1++;
            }));

        }
        private void UpdateSignalCh2(MyPackageInfo packageInfo, int i, int countPerPackage, int j)
        {
            //this.grid_stentCompleteSignalCh2.BeginEdit();
            //this.grid_stentCompleteSignalCh2.Rows.AddNew();
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[0].Value = channelDataCh2.RevCount + 1;
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[1].Value = packageInfo.Data[i].ToString("X2");//status
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[2].Value = packageInfo.Data[i + 1].ToString("X2");//data1
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[3].Value = packageInfo.Data[i + 2].ToString("X2");//data2
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[4].Value = packageInfo.Data[i + 3].ToString("X2");//data3
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[5].Value = packageInfo.Data[i + 4].ToString("X2");//data4
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[6].Value = packageInfo.Data[i + 5].ToString("X2");//data5
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[7].Value = packageInfo.Data[i + 6].ToString("X2");//data6
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[8].Value = packageInfo.Data[i + 7].ToString("X2");//crc
            //int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
            //this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[9].Value = Crc4_Cal(crcList);
            //this.grid_stentCompleteSignalCh2.EndEdit();
            ////清除数据到最大缓存
            //if (this.grid_stentCompleteSignalCh2.RowCount > cacheFrameNumber)
            //{
            //    this.grid_stentCompleteSignalCh2.Rows[0].Delete();
            //    channelDataCh2.RevCount -= 1;
            //    int id = 1;
            //    foreach (var rowInfo in this.grid_stentCompleteSignalCh2.Rows)
            //    {
            //        rowInfo.Cells[0].Value = id;
            //        id++;
            //    }
            //}
            //if (cacheCountCh2 == 50)
            //{
            //    this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].IsSelected = true;
            //    this.grid_stentCompleteSignalCh2.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh2.Rows.Count);
            //    this.grid_stentCompleteSignalCh2.Update();
            //    cacheCountCh2 = 0;
            //}
            //else if (channelDataCh2.ReceivePackageInfoQueue.Count <= 1 && (countPerPackage - j) <= 50)
            //{
            //    this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].IsSelected = true;
            //    this.grid_stentCompleteSignalCh2.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh2.Rows.Count);
            //    this.grid_stentCompleteSignalCh2.Update();
            //}
            //cacheCountCh2++;
        }
        #endregion

        private void CheckRegister()
        {
            try
            {
                DateTime daytime = DateTime.Parse(System.DateTime.Now.ToLongDateString());
                var path = AppDomain.CurrentDomain.BaseDirectory + "rs.ini";
                if (!File.Exists(path))
                {
                    DateTime usetime = daytime.AddDays(30);
                    var date = usetime.ToString("yyyy-MM-dd HH:mm:ss");
                    date = MySecurity.EncodeBase64(date);
                    INIFile.SetValue("s", "k",date , path);
                    FileInfo fileInfo = new FileInfo(path);
                    fileInfo.Attributes = FileAttributes.Hidden;
                    MessageBox.Show("感谢您使用本软件，您将有30天的试用期！", "提示",MessageBoxButtons.OK);
                }
                else
                {
                    //判断剩余天数
                    var date = INIFile.GetValue("s", "k", path);
                    date = MySecurity.DecodeBase64(date);
                    var currentDate = DateTime.Parse(date);
                    TimeSpan ts = currentDate - daytime;
                    int day = ts.Days;
                    if (day <= 0)
                    {
                        if (MessageBox.Show("软件试用期已到，请联系系统管理员！", "提示", MessageBoxButtons.OK) == DialogResult.OK)
                        {
                            //Environment.Exit(0);
                        }
                    }
                    else
                    {
                        MessageBox.Show("本软件的试用期还有" + day.ToString() + "天！", "提示");
                    }
                }
            }
            catch(Exception ex)
            {
            }
        }
    }
}
