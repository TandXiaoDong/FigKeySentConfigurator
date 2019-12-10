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
using SentProt.ClientSocket;
using SentProt.ClientSocket.AppBase;
using CommonUtils.Logger;
using WindowsFormTelerik.ControlCommon;
using CommonUtils.ByteHelper;
using CommonUtils.FileHelper;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using WindowsFormTelerik.GridViewExportData;
using WindowsFormTelerik.CommonUI;
using SentProt.Model;

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
        #region 公有变量
        private string serverIP;
        private int serverPort;
        private string stentConfigDirectory;
        private int cacheFrameNumber;//显示帧数
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
        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Init();
            EventHandlers();
        }

        private void EventHandlers()
        {
            timerCh1 = new System.Timers.Timer();
            timerCh2 = new System.Timers.Timer();
            timerCh1.Elapsed += TimerCh1_Elapsed;
            timerCh2.Elapsed += TimerCh2_Elapsed;
            this.menu_connectServer.Click += Menu_connectServer_Click;
            this.menu_exit.Click += Menu_exit_Click;
            this.menu_disconnect.Click += Menu_disconnect_Click;
            this.menu_export.Click += Menu_export_Click;
            this.menu_channel1.Click += Menu_channel1_Click;
            this.menu_channel2.Click += Menu_channel2_Click;
            this.menu_allChannel.Click += Menu_allChannel_Click;
            this.tool_connectServer.Click += Tool_connectServer_Click;
            this.tool_disconnect.Click += Tool_disconnect_Click;
            this.tool_continue.Click += Tool_continue_Click;
            this.tool_pause.Click += Tool_pause_Click;
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
            this.tool_channel2Export.Click += Tool_channel2Export_Click;
            this.rb_highBefore1Ch1.CheckStateChanged += Rb_highBefore_CheckStateChanged;
            this.rb_highBefore2Ch1.CheckStateChanged += Rb_highBefore2_CheckStateChanged;
            this.rb_highBefore1Ch2.CheckStateChanged += Rb_highBefore1Ch2_CheckStateChanged;
            this.rb_highBefore2Ch2.CheckStateChanged += Rb_highBefore2Ch2_CheckStateChanged;
            SuperEasyClient.NoticeConnectEvent += SuperEasyClient_NoticeConnectEvent;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            this.FormClosed += MainForm_FormClosed;
            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
        }

        private void Menu_allChannel_Click(object sender, EventArgs e)
        {
            
        }

        private void Menu_channel2_Click(object sender, EventArgs e)
        {
            this.radDock1.ActiveWindow = this.documentChannel2;
        }

        private void Menu_channel1_Click(object sender, EventArgs e)
        {
            this.radDock1.AddDocument(this.documentChannel1);
            //this.documentChannel1.Show();
            //this.radDock1.ActiveWindow = this.documentChannel1;
        }

        private void TimerCh1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendMessageCh1();
        }

        private void TimerCh2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendMessageCh2();
        }

        private void Tool_channel2Export_Click(object sender, EventArgs e)
        {
            ExportGridData(this.grid_stentCompleteSignalCh2, this.tool_channel2ExportFormat);
        }

        private void Tool_channel1Export_Click(object sender, EventArgs e)
        {
            ExportGridData(this.grid_stentCompleteSignalCh1,this.tool_channel1ExportFormat);
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
            this.grid_stentCompleteSignalCh2.Rows.Clear();
            this.grid_stentQuickBothCh2.Rows.Clear();
            this.grid_stentSlowSignalCh2.Rows.Clear();
            channelDataCh2.RevCount = 0;
            channelDataCh2.SlowSignalCount = 0;
        }

        private void Tool_channel1Clear_Click(object sender, EventArgs e)
        {
            this.grid_stentCompleteSignalCh1.Rows.Clear();
            this.grid_stentQuickBothCh1.Rows.Clear();
            this.grid_stentSlowSignalCh1.Rows.Clear();
            channelDataCh1.RevCount = 0;
            channelDataCh1.SlowSignalCount = 0;
        }

        private void Tool_channel2Stop_Click(object sender, EventArgs e)
        {
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求停止发送数据
            SuperEasyClient.SendMessage(StentSignalEnum.StopDataCh1, new byte[] { });
        }

        private void Tool_channel1stop_Click(object sender, EventArgs e)
        {
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求停止发送数据
            SuperEasyClient.SendMessage(StentSignalEnum.StopDataCh2, new byte[] { });
        }

        private void Tool_channel2Send_Click(object sender, EventArgs e)
        {
            SendMessage(StentSignalEnum.RequestDataCh2);
        }

        private void Tool_channel1Send_Click(object sender, EventArgs e)
        {
            SendMessage(StentSignalEnum.RequestDataCh1);
        }

        private void SendMessage(StentSignalEnum stentSignalEnum)
        {
            if (SuperEasyClient.client == null)
                return;
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求发送数据
            SuperEasyClient.SendMessage(stentSignalEnum, new byte[] { });
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

        private void Menu_export_Click(object sender, EventArgs e)
        {
            if(this.radDock1.ActiveWindow == this.documentChannel1)
                ExportGridData(this.grid_stentCompleteSignalCh1, this.tool_channel1ExportFormat);
            else if(this.radDock1.ActiveWindow == this.documentChannel2)
                ExportGridData(this.grid_stentCompleteSignalCh2, this.tool_channel2ExportFormat);
        }

        private void ExportGridData(RadGridView radGridView,ToolStripComboBox cbFormat)
        {
            if (this.grid_stentCompleteSignalCh1.RowCount < 1)
            {
                MessageBox.Show("没有可以导出的数据!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GridViewExport.ExportFormat currentExportType = GridViewExport.ExportFormat.EXCEL;
            if (cbFormat.SelectedItem == null)
            {
                MessageBox.Show("请选择导出格式!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Enum.TryParse(cbFormat.SelectedItem.ToString(), out currentExportType);
            GridViewExport.ExportGridViewData(currentExportType, radGridView);
        }

        private void SuperEasyClient_NoticeConnectEvent(bool IsConnect)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (IsConnect)
                {
                    this.tool_connectServer.Enabled = false;
                    this.tool_disconnect.Enabled = true;
                }
                else
                {
                    this.tool_connectServer.Enabled = true;
                    this.tool_disconnect.Enabled = false;
                    this.tool_continue.Enabled = false;
                    this.tool_pause.Enabled = false;
                }
            }));
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveStentConfig();
        }

        private void SetAutoSendSet()
        {
            int autoSendTimeInternal = 0;
            bool IsAutoSend = false;
            if (this.radDock1.ActiveWindow == this.documentChannel1)
            {
                autoSendTimeInternal = channelDataCh1.AutoSendTimeInternal;
                IsAutoSend = channelDataCh1.IsAutoSend;
            }
            else if (this.radDock1.ActiveWindow == this.documentChannel2)
            {
                autoSendTimeInternal = channelDataCh2.AutoSendTimeInternal;
                IsAutoSend = channelDataCh2.IsAutoSend;
            }
            SendSet sendSet = new SendSet(autoSendTimeInternal,IsAutoSend);
            if (sendSet.ShowDialog() == DialogResult.OK)
            {
                if (this.radDock1.ActiveWindow == this.documentChannel1)
                {
                    channelDataCh1.AutoSendTimeInternal = SendSet.autoSendTimerInternal;
                    channelDataCh1.IsAutoSend = SendSet.IsApplyAutoSend;
                }
                else if (this.radDock1.ActiveWindow == this.documentChannel2)
                {
                    channelDataCh2.AutoSendTimeInternal = SendSet.autoSendTimerInternal;
                    channelDataCh2.IsAutoSend = SendSet.IsApplyAutoSend;
                }
                //已设置自动发送
                if (channelDataCh1.IsAutoSend)
                {
                    timerCh1.Interval = channelDataCh1.AutoSendTimeInternal;
                    timerCh1.Start();
                }
                else
                {
                    timerCh1.Stop();
                }
                if (channelDataCh2.IsAutoSend)
                {
                    timerCh2.Interval = channelDataCh2.AutoSendTimeInternal;
                    timerCh2.Start();
                }
                else
                {
                    timerCh2.Stop();
                }
            }
        }

        private void Tool_cacheFrameAmount_Click(object sender, EventArgs e)
        {
            FrameShow frameShow = new FrameShow(cacheFrameNumber);
            if (frameShow.ShowDialog() == DialogResult.OK)
            {
                cacheFrameNumber = FrameShow.frameNumber;
            }
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
                this.tool_continue.Enabled = false;
                this.tool_pause.Enabled = false;
            }
        }

        private void SuperEasyClient_NoticeMessageEvent(MyPackageInfo packageInfo)
        {
            //this.grid_stentCompleteSignal.BeginInvoke(actions, packageInfo);
            this.grid_stentCompleteSignalCh1.BeginInvoke(new Action(()=>
            {
                RefreshGridData(packageInfo);
            }));
        }

        private void Menu_connectServer_Click(object sender, EventArgs e)
        {
            ConnectServerView();
        }

        private async void RefreshGridData(MyPackageInfo packageInfo)
        {
            await Task.Run(()=>
            {
                var header = BitConverter.ToInt16(packageInfo.Header, 0);
                var headerCh1 = (int)StentSignalEnum.RequestDataCh1;
                var headerCh2 = (int)StentSignalEnum.RequestDataCh2;
                if (header == headerCh1)
                {
                    channelDataCh1.ReceivePackageInfoQueue.Enqueue(packageInfo);
                    while (channelDataCh1.PackageInfoQueueTemp.Count <= 0)
                    {
                        if (channelDataCh1.ReceivePackageInfoQueue.Count > 0)
                        {
                            channelDataCh1.PackageInfoQueueTemp.Enqueue(channelDataCh1.ReceivePackageInfoQueue.Dequeue());
                            if (channelDataCh1.IsFirstReceive)
                            {
                                channelDataCh1.IsFirstReceive = !channelDataCh1.IsFirstReceive;
                                AnalysisUsualSignalCh1(channelDataCh1.PackageInfoQueueTemp.Dequeue());
                            }
                            else
                            {
                                while (true)
                                {
                                    //等待任务完成执行
                                    if (channelDataCh1.IsAnalysisComplete)
                                    {
                                        channelDataCh1.IsAnalysisComplete = !channelDataCh1.IsAnalysisComplete;
                                        AnalysisUsualSignalCh1(channelDataCh1.PackageInfoQueueTemp.Dequeue());
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else if (header == headerCh2)
                {
                    channelDataCh2.ReceivePackageInfoQueue.Enqueue(packageInfo);
                    while (channelDataCh2.PackageInfoQueueTemp.Count <= 0)
                    {
                        if (channelDataCh2.ReceivePackageInfoQueue.Count > 0)
                        {
                            channelDataCh2.PackageInfoQueueTemp.Enqueue(channelDataCh2.ReceivePackageInfoQueue.Dequeue());
                            if (channelDataCh2.IsFirstReceive)
                            {
                                channelDataCh2.IsFirstReceive = !channelDataCh2.IsFirstReceive;
                                AnalysisUsualSignalCh2(channelDataCh2.PackageInfoQueueTemp.Dequeue());
                            }
                            else
                            {
                                while (true)
                                {
                                    //等待任务完成执行
                                    if (channelDataCh2.IsAnalysisComplete)
                                    {
                                        channelDataCh2.IsAnalysisComplete = !channelDataCh2.IsAnalysisComplete;
                                        AnalysisUsualSignalCh2(channelDataCh2.PackageInfoQueueTemp.Dequeue());
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            });
        }

        private async Task<bool> AnalysisUsualSignalCh1(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length / 8;
            if (count == 0)
                return false;//长度不足
            await Task.Run(()=>
            {
                for (int i = 0; i < count * 8; i += 8)
                {
                    this.grid_stentCompleteSignalCh1.Invoke(new Action(() =>
                    {
                        channelDataCh1.ChannelType = ChannelData.ChannelTypeEnum.Channel1;
                        var iData = AnalysisSlowSignalData(packageInfo, i,channelDataCh1);
                        this.grid_stentCompleteSignalCh1.BeginEdit();
                        this.grid_stentCompleteSignalCh1.Rows.AddNew();
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[0].Value = channelDataCh1.RevCount + 1;
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[1].Value = packageInfo.Data[i].ToString("X2");//status
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[2].Value = packageInfo.Data[i + 1].ToString("X2");//data1
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[3].Value = packageInfo.Data[i + 2].ToString("X2");//data2
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[4].Value = packageInfo.Data[i + 3].ToString("X2");//data3
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[5].Value = packageInfo.Data[i + 4].ToString("X2");//data4
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[6].Value = packageInfo.Data[i + 5].ToString("X2");//data5
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[7].Value = packageInfo.Data[i + 6].ToString("X2");//data6
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[8].Value = packageInfo.Data[i + 7].ToString("X2");//crc
                        int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].Cells[9].Value = Crc4_Cal(crcList);
                        this.grid_stentCompleteSignalCh1.EndEdit();
                        //清除数据到最大缓存
                        if (channelDataCh1.IsAutoSend && this.grid_stentCompleteSignalCh1.RowCount > cacheFrameNumber)
                        {
                            this.grid_stentCompleteSignalCh1.Rows[0].Delete();
                            channelDataCh1.RevCount -= 1;
                            int id = 1;
                            foreach (var rowInfo in this.grid_stentCompleteSignalCh1.Rows)
                            {
                                rowInfo.Cells[0].Value = id;
                                id++;
                            }
                        }
                        this.grid_stentCompleteSignalCh1.Rows[channelDataCh1.RevCount].IsSelected = true;
                        this.grid_stentCompleteSignalCh1.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh1.Rows.Count);
                        this.grid_stentCompleteSignalCh1.Update();
                    }));
                    AnalysisQuickSignalCh1();
                    channelDataCh1.RevCount++;
                }
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
                for (int i = 0; i < count * 8; i += 8)
                {
                    this.grid_stentCompleteSignalCh2.Invoke(new Action(() =>
                    {
                        channelDataCh2.ChannelType = ChannelData.ChannelTypeEnum.Channel2;
                        var iData = AnalysisSlowSignalData(packageInfo, i,channelDataCh2);
                        this.grid_stentCompleteSignalCh2.BeginEdit();
                        this.grid_stentCompleteSignalCh2.Rows.AddNew();
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[0].Value = channelDataCh2.RevCount + 1;
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[1].Value = packageInfo.Data[i].ToString("X2");//status
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[2].Value = packageInfo.Data[i + 1].ToString("X2");//data1
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[3].Value = packageInfo.Data[i + 2].ToString("X2");//data2
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[4].Value = packageInfo.Data[i + 3].ToString("X2");//data3
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[5].Value = packageInfo.Data[i + 4].ToString("X2");//data4
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[6].Value = packageInfo.Data[i + 5].ToString("X2");//data5
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[7].Value = packageInfo.Data[i + 6].ToString("X2");//data6
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[8].Value = packageInfo.Data[i + 7].ToString("X2");//crc
                        int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].Cells[9].Value = Crc4_Cal(crcList);
                        this.grid_stentCompleteSignalCh2.EndEdit();
                        //清除数据到最大缓存
                        if (channelDataCh2.IsAutoSend && this.grid_stentCompleteSignalCh2.RowCount > cacheFrameNumber)
                        {
                            this.grid_stentCompleteSignalCh2.Rows[0].Delete();
                            channelDataCh2.RevCount -= 1;
                            int id = 1;
                            foreach (var rowInfo in this.grid_stentCompleteSignalCh2.Rows)
                            {
                                rowInfo.Cells[0].Value = id;
                                id++;
                            }
                        }
                        this.grid_stentCompleteSignalCh2.Rows[channelDataCh2.RevCount].IsSelected = true;
                        this.grid_stentCompleteSignalCh2.TableElement.ScrollToRow(this.grid_stentCompleteSignalCh2.Rows.Count);
                        this.grid_stentCompleteSignalCh2.Update();
                    }));
                    AnalysisQuickSignalCh2();
                    channelDataCh2.RevCount++;
                }
            });
            channelDataCh2.IsAnalysisComplete = true;
            return true;
        }

        private bool IsStentStandardFrameType(List<int> result)
        {
            int[] standFrameList = new int[] { 1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
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

        private string AnalysisSlowSignalData(MyPackageInfo packageInfo,int index,ChannelData channelData)
        {
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
                    LogHelper.Log.Info("【扩展帧】CRC校验成功 " + sumCRCCal);
                    //一包数据解析完成
                    //开始显示一包数据
                    channelData.FrameType = ChannelData.FrameTypeEnum.ExtendFrame;
                    UpdateSlowSignalGridData(channelData,messageID,data,crcValue,sumCRCCal);
                }
                else
                {
                    LogHelper.Log.Info($"【扩展帧】CRC校验失败 sumCRCCal={sumCRCCal} crcValue={crcValue}");
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
                crcValue = string.Format("{0:X4}", Convert.ToInt32(crcValue,2));
                if (sumCRCValue == crcValue)
                {
                    LogHelper.Log.Info("【标准帧】校验成功 " + sumCRC);
                    //一包数据解析完成
                    //开始显示一包数据
                    channelData.FrameType = ChannelData.FrameTypeEnum.StandardFrame;
                    UpdateSlowSignalGridData(channelData, messageID, data, crcValue, sumCRCValue);
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

        private void UpdateSlowSignalGridData(ChannelData channelData,string messageID,string data,string crcValue,string sumCRCCal)
        {
            if (channelData.ChannelType == ChannelData.ChannelTypeEnum.Channel1)
            {
                this.grid_stentSlowSignalCh1.Invoke(new Action(() =>
                {
                    this.radDock1.ActiveWindow = this.documentChannel1;
                    this.grid_stentSlowSignalCh1.Rows.AddNew();
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[0].Value = channelData.SlowSignalCount + 1;
                    if (channelData.FrameType == ChannelData.FrameTypeEnum.StandardFrame)
                    {
                        this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[1].Value = "标准帧";
                    }
                    else if (channelData.FrameType == ChannelData.FrameTypeEnum.ExtendFrame)
                    {
                        this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[1].Value = "扩展帧";
                    }
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[2].Value = messageID;
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[3].Value = data;
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[4].Value = crcValue;
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].Cells[5].Value = sumCRCCal;
                    this.grid_stentSlowSignalCh1.Rows[channelData.SlowSignalCount].IsSelected = true;
                    this.grid_stentSlowSignalCh1.TableElement.ScrollToRow(this.grid_stentSlowSignalCh1.Rows.Count);
                    this.grid_stentSlowSignalCh1.Update();
                    channelData.SlowSignalCount++;
                }));
            }
            else if (channelData.ChannelType == ChannelData.ChannelTypeEnum.Channel2)
            {
                this.grid_stentSlowSignalCh2.Invoke(new Action(() =>
                {
                    this.radDock1.ActiveWindow = this.documentChannel2;
                    this.grid_stentSlowSignalCh2.Rows.AddNew();
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[0].Value = channelData.SlowSignalCount + 1;
                    if (channelData.FrameType == ChannelData.FrameTypeEnum.StandardFrame)
                    {
                        this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[1].Value = "标准帧";
                    }
                    else if (channelData.FrameType == ChannelData.FrameTypeEnum.ExtendFrame)
                    {
                        this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[1].Value = "扩展帧";
                    }
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[2].Value = messageID;
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[3].Value = data;
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[4].Value = crcValue;
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].Cells[5].Value = sumCRCCal;
                    this.grid_stentSlowSignalCh2.Rows[channelData.SlowSignalCount].IsSelected = true;
                    this.grid_stentSlowSignalCh2.TableElement.ScrollToRow(this.grid_stentSlowSignalCh2.Rows.Count);
                    this.grid_stentSlowSignalCh2.Update();
                    channelData.SlowSignalCount++;
                }));
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

        /*
         * 【快信号】
         * 1）显示最新数据
         * 2）根据高低位顺序，重新计算数据
         */
        private void AnalysisQuickSignalCh1()
        {
            //将显示数据最新一条数据，按高低位排序显示
            if (this.grid_stentCompleteSignalCh1.RowCount < 1)
                return;
            if (this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[2].Value == null)
                return;
            var latestValue = this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[2].Value.ToString() + this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[3].Value.ToString() + this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[4].Value.ToString() + this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[5].Value.ToString() + this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[6].Value.ToString() + this.grid_stentCompleteSignalCh1.Rows[this.grid_stentCompleteSignalCh1.Rows.Count - 1].Cells[7].Value.ToString();
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
                if (this.grid_stentQuickBothCh1.Rows.Count < 1)
                    this.grid_stentQuickBothCh1.Rows.AddNew();
                this.grid_stentQuickBothCh1.Rows[0].Cells[0].Value = BitConverter.ToString(data1).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBothCh1.Rows[0].Cells[1].Value = BitConverter.ToString(data2).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBothCh1.Rows[0].IsSelected = true;
                this.grid_stentQuickBothCh1.Update();
            }));
        }

        private void AnalysisQuickSignalCh2()
        {
            //将显示数据最新一条数据，按高低位排序显示
            if (this.grid_stentCompleteSignalCh2.RowCount < 1)
                return;
            if (this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[2].Value == null)
                return;
            var latestValue = this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[2].Value.ToString() + this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[3].Value.ToString() + this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[4].Value.ToString() + this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[5].Value.ToString() + this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[6].Value.ToString() + this.grid_stentCompleteSignalCh2.Rows[this.grid_stentCompleteSignalCh2.Rows.Count - 1].Cells[7].Value.ToString();
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
                if (this.grid_stentQuickBothCh2.Rows.Count < 1)
                    this.grid_stentQuickBothCh2.Rows.AddNew();
                this.grid_stentQuickBothCh2.Rows[0].Cells[0].Value = BitConverter.ToString(data1).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBothCh2.Rows[0].Cells[1].Value = BitConverter.ToString(data2).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBothCh2.Rows[0].IsSelected = true;
                this.grid_stentQuickBothCh2.Update();
            }));
        }

        private void ConnectServerView()
        {
            AddConnection addConnection = new AddConnection(this.serverIP,this.serverPort);
            if (addConnection.ShowDialog() == DialogResult.OK)
            {
                if (!SuperEasyClient.client.IsConnected)
                {
                    MessageBox.Show("连接服务失败！","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }
                //连接成功
                if (channelDataCh1.IsAutoSend)
                {
                    timerCh1.Interval = channelDataCh1.AutoSendTimeInternal;
                    timerCh1.Start();
                }
                if (channelDataCh2.IsAutoSend)
                {
                    timerCh2.Interval = channelDataCh2.AutoSendTimeInternal;
                    timerCh2.Start();
                }
                this.serverIP = AddConnection.serverIP;
                this.serverPort = AddConnection.serverPort;
                this.tool_continue.Enabled = true;
                this.tool_pause.Enabled = true;
                this.tool_channel1Send.Enabled = true;
                this.tool_channel2Send.Enabled = true;
                this.tool_channel1stop.Enabled = true;
                this.tool_channel2Stop.Enabled = true;
                this.tool_connectServer.Enabled = false;
                this.tool_disconnect.Enabled = true;
            }
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

        private void Init()
        {
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentCompleteSignalCh1,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentSlowSignalCh1,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickBothCh1, false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentCompleteSignalCh2, false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentSlowSignalCh2, false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickBothCh2, false);
            this.grid_stentCompleteSignalCh1.ReadOnly = true;
            this.grid_stentSlowSignalCh1.ReadOnly = true;
            this.grid_stentQuickBothCh1.ReadOnly = true;
            this.grid_stentCompleteSignalCh2.ReadOnly = true;
            this.grid_stentSlowSignalCh2.ReadOnly = true;
            this.grid_stentQuickBothCh2.ReadOnly = true;
            this.rb_highBefore1Ch1.CheckState = CheckState.Checked;
            this.rb_highBefore2Ch1.CheckState = CheckState.Checked;
            this.rb_highBefore1Ch2.CheckState = CheckState.Checked;
            this.rb_highBefore2Ch2.CheckState = CheckState.Checked;

            this.tool_continue.Enabled = false;
            this.tool_pause.Enabled = false;
            this.tool_channel1Send.Enabled = false;
            this.tool_channel2Send.Enabled = false;
            this.tool_channel1stop.Enabled = false;
            this.tool_channel2Stop.Enabled = false;

            this.tool_channel1ExportFormat.Items.Add(GridViewExport.ExportFormat.EXCEL);
            this.tool_channel1ExportFormat.Items.Add(GridViewExport.ExportFormat.HTML);
            this.tool_channel1ExportFormat.Items.Add(GridViewExport.ExportFormat.PDF);
            this.tool_channel1ExportFormat.Items.Add(GridViewExport.ExportFormat.CSV);
            this.tool_channel1ExportFormat.SelectedIndex = 0;
            //ch2
            this.tool_channel2ExportFormat.Items.Add(GridViewExport.ExportFormat.EXCEL);
            this.tool_channel2ExportFormat.Items.Add(GridViewExport.ExportFormat.HTML);
            this.tool_channel2ExportFormat.Items.Add(GridViewExport.ExportFormat.PDF);
            this.tool_channel2ExportFormat.Items.Add(GridViewExport.ExportFormat.CSV);
            this.tool_channel2ExportFormat.SelectedIndex = 0;

            this.grid_stentSlowSignalCh1.Columns[4].IsVisible = false;
            this.grid_stentSlowSignalCh1.Columns[5].IsVisible = false;
            this.grid_stentSlowSignalCh2.Columns[4].IsVisible = false;
            this.grid_stentSlowSignalCh2.Columns[5].IsVisible = false;

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
            channelDataCh1.IsAutoSend = IsAutoSend;

            //ch2 is auto send
            isAuto = INIFile.GetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_IS_AUTO_KEY, stentConfigPath);
            if (isAuto != "")
                bool.TryParse(isAuto, out IsAutoSend);
            channelDataCh2.IsAutoSend = IsAutoSend;

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
            INIFile.SetValue(STENT_CONFIG_SECTION_CH1, STENT_CONFIG_IS_AUTO_KEY,channelDataCh1.IsAutoSend.ToString(),stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION_CH1, STENT_CONFIG_TIME_INTERNAL,channelDataCh1.AutoSendTimeInternal.ToString(),stentConfigPath);
            //ch2
            INIFile.SetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_IS_AUTO_KEY, channelDataCh2.IsAutoSend.ToString(), stentConfigPath);
            INIFile.SetValue(STENT_CONFIG_SECTION_CH2, STENT_CONFIG_TIME_INTERNAL, channelDataCh2.AutoSendTimeInternal.ToString(), stentConfigPath);
        }

        private void SendMessageCh1()
        {
            SuperEasyClient.SendMessage(StentSignalEnum.RequestDataCh1, new byte[0]);
        }

        private void SendMessageCh2()
        {
            SuperEasyClient.SendMessage(StentSignalEnum.RequestDataCh2, new byte[0]);
        }
    }
}
