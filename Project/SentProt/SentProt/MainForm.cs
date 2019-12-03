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
using System.Threading.Tasks;
using System.Threading;

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
        private int slowSignalCount = 0;
        private bool IsFirstReceive = true;

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

            Other
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
            this.tool_disconnect.Click += Tool_disconnect_Click;
            this.tool_start.Click += Tool_start_Click;
            this.tool_stop.Click += Tool_stop_Click;
            this.cb_dataType.SelectedIndexChanged += Cb_dataType_SelectedIndexChanged;
            this.rb_highBefore.CheckStateChanged += Rb_highBefore_CheckStateChanged;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
        }

        private void Cb_dataType_SelectedIndexChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignal();
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

        private void Tool_disconnect_Click(object sender, EventArgs e)
        {
            SuperEasyClient.client.Close();
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

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            
        }

        private void AnalysisUsualSignal(MyPackageInfo packageInfo)
        {
            int count = packageInfo.Data.Length  / 8;
            if (count == 0)
                return;//长度不足
            Task task = new Task(()=>
            {
                for (int i = 0; i < count * 8; i += 8)
                {
                    this.grid_stentCompleteSignal.Invoke(new Action(() =>
                    {
                        var iData = ConvertReceiveData(packageInfo, i);
                        this.grid_stentCompleteSignal.BeginEdit();
                        this.grid_stentCompleteSignal.Rows.AddNew();
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[0].Value = revCount + 1;
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[1].Value = iData;
                        this.grid_stentCompleteSignal.Rows[revCount].IsSelected = true;
                        this.grid_stentCompleteSignal.TableElement.ScrollToRow(this.grid_stentCompleteSignal.Rows.Count);
                        this.grid_stentCompleteSignal.EndEdit();
                        this.grid_stentCompleteSignal.Update();
                    }));
                    revCount++;
                    Thread.Sleep(100);
                }
                AnalysisQuickSignal();
            });
            task.Start();
        }

        private string ConvertReceiveData(MyPackageInfo packageInfo,int index)
        {
            var iData = ""; 
            if (this.tool_dataType.SelectedIndex == 0)
            {
                iData = packageInfo.Data[index].ToString("X2") + "-" + packageInfo.Data[index + 1].ToString("X2") + "-" + packageInfo.Data[index + 2].ToString("X2") + "-" + packageInfo.Data[index + 3].ToString("X2") + "-" + packageInfo.Data[index + 4].ToString("X2") + "-" + packageInfo.Data[index + 5].ToString("X2") + "-" + packageInfo.Data[index + 6].ToString("X2") + "-" + packageInfo.Data[index + 7].ToString("X2");
                var firstByteString = Convert.ToString(packageInfo.Data[index],2).PadLeft(4,'0');
                var firstBit = firstByteString.Substring(firstByteString.Length - 4,1);//从右往左数，起始位为0，第3位
                if (IsFirstReceive)
                {
                    IsFirstReceive = false;
                    if (firstBit != "1")
                    {
                        IsFirstReceive = true;
                        return iData;
                    }
                }
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
                    else
                    {
                        currentFrameType = MessageFormatType.Other;
                        cacheDataPerFrame.Clear();
                        cacheFirstBitValue.Clear();
                    }
                    //数据帧类型判断结束

                    if (currentFrameType == MessageFormatType.ExtendedFrame)
                    {
                        //扩展帧为18帧为一完整包
                        if (cacheDataPerFrame.Count >= 18)
                        {
                            //起始位为0的第二位与第三位
                            var messageID = "";
                            var data = "";
                            var dataMessageID = "";//message id剩余部分
                            int count = 0;
                            var sumCRC = "";
                            var crcValue = "";
                            foreach (var signalData in this.cacheDataPerFrame)
                            {
                                var bitData = Convert.ToString(Convert.ToByte(signalData.Substring(0,2), 16), 2).PadLeft(4,'0');
                                if (cacheFirstBitValue[7] == "0")
                                {
                                    //this is 12-bit-data and 8-bit-message-id
                                    //index:6-17
                                    if (count >= 6 && count <= 17)
                                    {
                                        data += bitData.Substring(bitData.Length - 3,1);
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
                                else if (cacheFirstBitValue[7] == "1")
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
                                            dataMessageID += bitData.Substring(bitData.Length - 4,1);
                                        }
                                    }
                                }
                                //计算CRC
                                if (count >= 0 && count <= 5)
                                {
                                    crcValue += bitData.Substring(bitData.Length - 3, 1);
                                }
                                if (count >= 6 && count >= 17)
                                {
                                    sumCRC += bitData.Substring(bitData.Length - 3, 1) + bitData.Substring(bitData.Length - 4, 1);
                                }
                                count++;
                            }
                            data += dataMessageID;
                            data = "0X"+string.Format("{0:x2}", Convert.ToInt32(data, 2));
                            messageID = "0X"+string.Format("{0:x2}",Convert.ToInt32(messageID,2));
                            crcValue = string.Format("{0:x2}",Convert.ToInt32(crcValue,2));
                            var sumCRCCal = Crc6_Cal(AddBit2Array(sumCRC));
                            if (Convert.ToInt32(crcValue) == sumCRCCal)
                            {
                                LogHelper.Log.Info("【扩展帧】CRC校验成功 "+sumCRCCal);
                            }
                            else
                            {
                                LogHelper.Log.Info($"【扩展帧】CRC校验失败 sumCRCCal={sumCRCCal} crcValue={crcValue}");
                            }
                            //一包数据解析完成
                            //开始显示一包数据
                            this.grid_stentSlowSignal.Invoke(new Action(() =>
                            {
                                this.grid_stentSlowSignal.Rows.AddNew();
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[0].Value = slowSignalCount + 1;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[1].Value = "扩展帧";
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[2].Value = messageID;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[3].Value = data;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].IsSelected = true;
                                this.grid_stentSlowSignal.TableElement.ScrollToRow(this.grid_stentSlowSignal.Rows.Count);
                                this.grid_stentSlowSignal.Update();
                            }));

                            //清空缓存
                            cacheDataPerFrame.Clear();
                            cacheFirstBitValue.Clear();
                            IsFirstReceive = true;
                            slowSignalCount++;
                        }
                    }
                    else if (currentFrameType == MessageFormatType.StandardFrame)
                    {
                        //标准帧为16帧为一个完整包
                        if (cacheDataPerFrame.Count >= 16)
                        {
                            //获取message ID 与data  起始位为0的第2位
                            var messageID = "";
                            var data = "";
                            int count = 0;
                            var sumCRC = "";
                            var crcValue = "";
                            foreach (var signalData in this.cacheDataPerFrame)
                            {
                                var bitData = Convert.ToString(Convert.ToByte(signalData.Substring(0, 2), 16), 2).PadLeft(4,'0');//4位bit
                                if (count <= 3)
                                {
                                    messageID += bitData.Substring(bitData.Length - 3, 1);
                                }
                                if (count >= 4 && count <= 11)
                                {
                                    data += bitData.Substring(bitData.Length - 3,1);
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
                            data = "0X"+string.Format("{0:x2}", Convert.ToInt32(data, 2));
                            messageID = "0X"+string.Format("{0:x2}", Convert.ToInt32(messageID, 2));
                            sumCRC = string.Format("{0:x2}",Convert.ToInt32(sumCRC, 2));
                            crcValue = string.Format("{0:x2}",Convert.ToInt32(crcValue));
                            if (sumCRC == crcValue)
                            {
                                LogHelper.Log.Info("【标准帧】校验成功 "+sumCRC);
                            }
                            else
                            {
                                LogHelper.Log.Info("【标准帧】校验失败 " + crcValue);
                            }
                            //一包数据解析完成
                            //开始显示一包数据
                            this.grid_stentSlowSignal.Invoke(new Action(() =>
                            {
                                this.grid_stentSlowSignal.Rows.AddNew();
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[0].Value = slowSignalCount + 1;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[1].Value = "标准帧";
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[2].Value = messageID;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[3].Value = data;
                                this.grid_stentSlowSignal.Rows[slowSignalCount].IsSelected = true;
                                this.grid_stentSlowSignal.TableElement.ScrollToRow(this.grid_stentSlowSignal.Rows.Count);
                                this.grid_stentSlowSignal.Update();
                            }));
                            //清空缓存
                            cacheDataPerFrame.Clear();
                            cacheFirstBitValue.Clear();
                            IsFirstReceive = true;
                            slowSignalCount++;
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
            ChangeQuickDataShowType();
            if (this.grid_stentCompleteSignal.RowCount < 1)
                return;
            var latestValue = this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[1].Value.ToString();
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
            this.grid_stentQuickSignal.Invoke(new Action(()=>
            {
                if (this.cb_dataType.SelectedIndex == 0)
                {
                    //只显示数据1
                    if (this.grid_stentQuickSignal.Rows.Count < 1)
                        this.grid_stentQuickSignal.Rows.AddNew();
                    var data = BitConverter.ToString(data1).Replace("0","").Replace("-","");
                    this.grid_stentQuickSignal.Rows[0].Cells[0].Value = data;
                    this.grid_stentQuickSignal.Rows[0].IsSelected = true;
                    this.grid_stentQuickSignal.Update();
                }
                else
                {
                    //显示数据1与数据2
                    if (this.grid_stentQuickBoth.Rows.Count < 1)
                        this.grid_stentQuickBoth.Rows.AddNew();
                    this.grid_stentQuickBoth.Rows[0].Cells[0].Value = BitConverter.ToString(data1).Replace("0","").Replace("-","");
                    this.grid_stentQuickBoth.Rows[0].Cells[1].Value = BitConverter.ToString(data2).Replace("0","").Replace("-","");
                    this.grid_stentQuickBoth.Rows[0].IsSelected = true;
                    this.grid_stentQuickBoth.Update();
                }
            }));
        }

        private void ConnectServerView()
        {
            AddConnection addConnection = new AddConnection();
            addConnection.ShowDialog();
        }

        private int[] AddBit2Array(string crcSum)
        {
            if (crcSum.Length != 24)
                return new int[] { };
            int[] crcArray = new int[4];
            crcArray[0] = Convert.ToInt32(crcSum.Substring(0, 6));
            crcArray[0] = Convert.ToInt32(crcSum.Substring(6, 6));
            crcArray[0] = Convert.ToInt32(crcSum.Substring(12, 6));
            crcArray[0] = Convert.ToInt32(crcSum.Substring(18, 6));
            return crcArray;
        }

        private int Crc6_Cal(int[] dataResult)/*data位6位nibble块的值，len为数据块的数量*/
        {
            /*crc初始值*/
            var result = 0x15;
            /*查表地址*/
            var tableNo = 0;

            /*对额外添加的6个0进行查表计算crc*/
            tableNo = result ^ 0;
            result = dataResult[tableNo];

            /*对数组数据查表计算crc*/
            for (int i = 0; i < dataResult.Length; i++)
            {
                tableNo = result ^ dataResult[i];
                result = dataResult[tableNo];
            }

            /*返回最终的crc值*/
            return result;
        }

        private void Init()
        {
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentCompleteSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentSlowSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickBoth, false);
            this.grid_stentCompleteSignal.ReadOnly = true;
            this.grid_stentQuickSignal.ReadOnly = true;
            this.grid_stentSlowSignal.ReadOnly = true;
            this.tool_dataType.Items.Add("16进制");
            this.tool_dataType.Items.Add("二进制");
            this.tool_dataType.SelectedIndex = 0;
            this.rb_highBefore.CheckState = CheckState.Checked;
            cacheDataPerFrame = new List<string>();
            this.cb_dataType.SelectedIndex = 1;
            ChangeQuickDataShowType();
        }

        private void ChangeQuickDataShowType()
        {
            if (this.cb_dataType.SelectedIndex == 0)
            {
                this.grid_stentQuickBoth.Visible = false;
                this.grid_stentQuickSignal.Visible = true;
                this.grid_stentQuickSignal.Dock = DockStyle.Fill;
            }
            else
            {
                this.grid_stentQuickSignal.Visible = false;
                this.grid_stentQuickBoth.Visible = true;
                this.grid_stentQuickBoth.Dock = DockStyle.Fill;
            }
        }
    }
}
