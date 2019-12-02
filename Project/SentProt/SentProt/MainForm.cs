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
        private List<string> cacheDataPerFrame;
        private List<string> cacheFirstBitValue = new List<string>();
        private MessageFormatType currentFrameType  = MessageFormatType.StandardFrame;

        private enum MessageFormatType
        {
            /// <summary>
            /// 标准帧
            /// </summary>
            StandardFrame,
           /// <summary>
           /// 扩展帧
           /// </summary>
            ExtendedFrame,

        }

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
            this.tool_start.Click += Tool_start_Click;
            this.tool_stop.Click += Tool_stop_Click;
            this.rb_highBefore.CheckStateChanged += Rb_highBefore_CheckStateChanged;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
        }

        private void Rb_highBefore_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignal();
        }

        private void Tool_stop_Click(object sender, EventArgs e)
        {
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求停止发送数据
            SuperEasyClient.SendMessage(StentSignalEnum.StopData,new byte[] { });
        }

        private void Tool_start_Click(object sender, EventArgs e)
        {
            if (!SuperEasyClient.client.IsConnected)
                return;
            //请求发送数据
            SuperEasyClient.SendMessage(StentSignalEnum.RequestData,new byte[] { });
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
        }

        private void AnalysisUsualSignal(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length  / 8;
            if (count == 0)
                return;//长度不足
            for (int i = 0; i < count * 8; i += 8)
            {
                var iData = ConvertReceiveData(packageInfo,i);
                this.grid_stentCompleteSignal.Rows.AddNew();
                this.grid_stentCompleteSignal.Rows[revCount].Cells[0].Value = revCount + 1;
                this.grid_stentCompleteSignal.Rows[revCount].Cells[1].Value = packageInfo.Data.Length;
                this.grid_stentCompleteSignal.Rows[revCount].Cells[2].Value = iData;
                this.grid_stentCompleteSignal.Rows[revCount].IsSelected = true;
                this.grid_stentCompleteSignal.TableElement.ScrollToRow(this.grid_stentCompleteSignal.Rows.Count);
                revCount++;
                System.Threading.Thread.Sleep(100);
            }
            AnalysisQuickSignal();
        }

        private string ConvertReceiveData(MyPackageInfo packageInfo,int index)
        {
            var iData = ""; 
            if (this.tool_dataType.SelectedIndex == 0)
            {
                iData = packageInfo.Data[index].ToString("X2") + "-" + packageInfo.Data[index + 1].ToString("X2") + "-" + packageInfo.Data[index + 2].ToString("X2") + "-" + packageInfo.Data[index + 3].ToString("X2") + "-" + packageInfo.Data[index + 4].ToString("X2") + "-" + packageInfo.Data[index + 5].ToString("X2") + "-" + packageInfo.Data[index + 6].ToString("X2") + "-" + packageInfo.Data[index + 7].ToString("X2");
                var firstByteString = Convert.ToString(packageInfo.Data[index],2);
                var firstBit = firstByteString.Substring(firstByteString.Length - 4,1);//从右往左数，起始位为0，第3位
                cacheFirstBitValue.Add(firstBit);
                cacheDataPerFrame.Add(iData);
                //开始判断数据类型：标准帧类/扩展帧类型
                if (cacheFirstBitValue.Count >= 8)
                {
                    //判断数据类型最终结果
                    //前6个第三位bit为1，第7位bit为0--------为扩展类型
                    if (cacheFirstBitValue[0] == "1" && cacheFirstBitValue[1] == "1" && cacheFirstBitValue[2] == "1" && cacheFirstBitValue[3] == "1" && cacheFirstBitValue[4] == "1" && cacheFirstBitValue[5] == "1" && cacheFirstBitValue[6] == "0")
                    {
                        currentFrameType = MessageFormatType.ExtendedFrame;
                    }
                    else if (cacheFirstBitValue[0] == "1" && cacheFirstBitValue[1] == "0" && cacheFirstBitValue[2] == "0")
                    {
                        //第一位为1，第2/3/4位为0
                        currentFrameType = MessageFormatType.StandardFrame;
                    }
                    //数据帧类型判断结束
                    if (currentFrameType == MessageFormatType.ExtendedFrame)
                    {
                        //扩展帧为18帧为一完整包
                        if (cacheFirstBitValue[7] == "0")
                        {
                        }
                        else if (cacheFirstBitValue[7] == "1")
                        { 
                        }
                    }
                    else if (currentFrameType == MessageFormatType.StandardFrame)
                    {
                        //标准帧为16帧为一个完整包
                        if (cacheDataPerFrame.Count >= 16)
                        {
                            //获取message ID 与data
                            var messageID = "";
                            var data = "";
                            int count = 0;
                            foreach (var signalData in this.cacheDataPerFrame)
                            {
                                var bitData = Convert.ToString(Convert.ToByte(signalData,16),2);//4位bit
                                if (count <= 3)
                                {
                                    messageID += bitData.Substring(bitData.Length - 3, 1);
                                }
                                if (count >= 4 && count <= 11)
                                {
                                    data += bitData.Substring(bitData.Length - 3,1);
                                }
                                count++;
                            }
                            //一包数据解析完成
                            //开始显示一包数据
                            this.grid_stentSlowSignal.Rows.AddNew();
                            this.grid_stentSlowSignal.Rows[revCount].Cells[0].Value = revCount + 1;
                            this.grid_stentSlowSignal.Rows[revCount].Cells[1].Value = packageInfo.Data.Length;
                            this.grid_stentSlowSignal.Rows[revCount].Cells[2].Value = messageID;
                            this.grid_stentSlowSignal.Rows[revCount].Cells[3].Value = data;
                            this.grid_stentSlowSignal.Rows[revCount].IsSelected = true;
                            this.grid_stentSlowSignal.TableElement.ScrollToRow(this.grid_stentSlowSignal.Rows.Count);
                            //清空缓存
                            cacheDataPerFrame.Clear();
                            cacheFirstBitValue.Clear();
                        }
                    }
                }
            }
            else if (this.tool_dataType.SelectedIndex == 1)
                iData = Convert.ToString(packageInfo.Data[index], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 1], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 2], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 3], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 4], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 5], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 6], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 7], 2).PadLeft(4, '0');
            return iData;
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
        private void AnalysisQuickSignal()
        {
            //将显示数据最新一条数据，按高低位排序显示
            var latestValue = this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[2].Value.ToString();
            byte[] latestByte = ConvertByte.HexToByte(latestValue,2,2);
            byte[] data1 = new byte[3];
            byte[] data2 = new byte[3];
            Array.Copy(latestByte,0,data1,0,3);
            Array.Copy(latestByte,3,data2,0,3);
            if (rb_highBefore.CheckState == CheckState.Checked)
            {

            }
            else if(rb_lowerBefore.CheckState == CheckState.Checked)
            {
                data1 = data1.Reverse().ToArray();
                data2 = data2.Reverse().ToArray();
            }
            if(this.grid_stentQuickSignal.Rows.Count < 1)
                this.grid_stentQuickSignal.Rows.AddNew();
            this.grid_stentQuickSignal.Rows[0].Cells[0].Value = 1;
            this.grid_stentQuickSignal.Rows[0].Cells[1].Value = 6;
            this.grid_stentQuickSignal.Rows[0].Cells[2].Value = BitConverter.ToString(data1) + BitConverter.ToString(data2);
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
            this.tool_dataType.Items.Add("16进制");
            this.tool_dataType.Items.Add("二进制");
            this.tool_dataType.SelectedIndex = 0;
            this.rb_highBefore.CheckState = CheckState.Checked;
            cacheDataPerFrame = new List<string>();
        }
    }
}
