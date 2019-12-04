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
        private List<int> cacheFirstBitValue = new List<int>();
        private MessageFormatType currentFrameType  = MessageFormatType.StandardFrame;
        private int slowSignalCount = 0;
        private bool IsFirstReceive = true;
        private int cacheFrameNumber;
        private string stentConfigDirectory;
        private const string STENT_CONFIG_FILE = "stentConfig.ini";
        private const string STENT_CONFIG_SECTION = "STENT";
        private const string STENT_CONFIG_FRAME_COUNT_KEY = "frameCount";

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
            this.tool_cacheFrameAmount.Click += Tool_cacheFrameAmount_Click;
            this.rb_highBefore1.CheckStateChanged += Rb_highBefore_CheckStateChanged;
            this.rb_highBefore2.CheckStateChanged += Rb_highBefore2_CheckStateChanged;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
            this.FormClosed += MainForm_FormClosed;
            //this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count-1].Index;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveStentConfig();
        }

        private void Tool_cacheFrameAmount_Click(object sender, EventArgs e)
        {
            FrameShow frameShow = new FrameShow(cacheFrameNumber);
            if (frameShow.ShowDialog() == DialogResult.OK)
            {
                cacheFrameNumber = FrameShow.frameNumber;
            }
        }

        private void Rb_highBefore_CheckStateChanged(object sender, EventArgs e)
        {
            AnalysisQuickSignal();
        }

        private void Rb_highBefore2_CheckStateChanged(object sender, EventArgs e)
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
            if (!SuperEasyClient.client.IsConnected)
            {
                //已断开连接
                this.tool_disconnect.Enabled = false;
                this.tool_connectServer.Enabled = true;
            }
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
                        var iData = AnalysisSlowSignalData(packageInfo, i);
                        this.grid_stentCompleteSignal.BeginEdit();
                        this.grid_stentCompleteSignal.Rows.AddNew();
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[0].Value = revCount + 1;
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[1].Value = packageInfo.Data[i].ToString("X2");//status
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[2].Value = packageInfo.Data[i + 1].ToString("X2");//data1
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[3].Value = packageInfo.Data[i + 2].ToString("X2");//data2
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[4].Value = packageInfo.Data[i + 3].ToString("X2");//data3
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[5].Value = packageInfo.Data[i + 4].ToString("X2");//data4
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[6].Value = packageInfo.Data[i + 5].ToString("X2");//data5
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[7].Value = packageInfo.Data[i + 6].ToString("X2");//data6
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[8].Value = packageInfo.Data[i + 7].ToString("X2");//crc
                        int[] crcList = new int[] { packageInfo.Data[i + 1], packageInfo.Data[i + 2], packageInfo.Data[i + 3], packageInfo.Data[i + 4], packageInfo.Data[i + 5], packageInfo.Data[i + 6] };
                        this.grid_stentCompleteSignal.Rows[revCount].Cells[9].Value = Crc4_Cal(crcList);
                        this.grid_stentCompleteSignal.Rows[revCount].IsSelected = true;
                        this.grid_stentCompleteSignal.TableElement.ScrollToRow(this.grid_stentCompleteSignal.Rows.Count);
                        this.grid_stentCompleteSignal.EndEdit();
                        this.grid_stentCompleteSignal.Update();
                    }));
                    AnalysisQuickSignal();
                    revCount++;
                    Task.Delay(50);
                }
            });
            task.Start();
        }

        private bool IsStentStandardFrameType(List<int> result)
        {
            int[] standFrameList = new int[] { 1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
            //判断标准帧
            
        }

        private bool IsStentExtentFrameType(List<int> result)
        {
            int[] extendFrameList = new int[] { 1, 1, 1, 1, 1, 1, 0, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, 0 };
            //判断扩展帧
            //7/13/18帧对应值为0，1-6帧对应值为1
        }

        private string AnalysisSlowSignalData(MyPackageInfo packageInfo,int index)
        {
            var iData = packageInfo.Data[index].ToString("X2") + "-" + packageInfo.Data[index + 1].ToString("X2") + "-" + packageInfo.Data[index + 2].ToString("X2") + "-" + packageInfo.Data[index + 3].ToString("X2") + "-" + packageInfo.Data[index + 4].ToString("X2") + "-" + packageInfo.Data[index + 5].ToString("X2") + "-" + packageInfo.Data[index + 6].ToString("X2") + "-" + packageInfo.Data[index + 7].ToString("X2");
            var firstByteString = Convert.ToString(packageInfo.Data[index], 2).PadLeft(4, '0');
            var firstBit = firstByteString.Substring(firstByteString.Length - 4, 1);//从右往左数，起始位为0，第3位
            if (IsFirstReceive)
            {
                IsFirstReceive = false;
                if (firstBit != "1")
                {
                    IsFirstReceive = true;
                    return iData;
                }
            }
            cacheFirstBitValue.Add(int.Parse(firstBit));
            cacheDataPerFrame.Add(iData);
            //当小于16帧或18帧时，如果既不满足标准帧，也不满足扩展帧，全部缓存清零
            if (cacheFirstBitValue.Count <= 16)
            {

            }
            else if (cacheFirstBitValue.Count <= 18)
            { 
            }
            //当

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
                            var bitData = Convert.ToString(Convert.ToByte(signalData.Substring(0, 2), 16), 2).PadLeft(4, '0');
                            if (cacheFirstBitValue[7] == "0")
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
                        data += dataMessageID;
                        data = "0X" + string.Format("{0:x2}", Convert.ToInt32(data, 2));
                        messageID = "0X" + string.Format("{0:x2}", Convert.ToInt32(messageID, 2));
                        crcValue = string.Format("{0:X4}", Convert.ToInt32(crcValue, 2));
                        var sumCRCCal = Crc6_Cal(Add6Bit2Array(sumCRC));
                        if (crcValue == sumCRCCal)
                        {
                            LogHelper.Log.Info("【扩展帧】CRC校验成功 " + sumCRCCal);
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
                            this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[4].Value = crcValue;
                            this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[5].Value = sumCRCCal;
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
                        var sumCRCValue = "";
                        var crcValue = "";
                        foreach (var signalData in this.cacheDataPerFrame)
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
                        crcValue = string.Format("{0:X4}", Convert.ToInt32(crcValue));
                        if (sumCRCValue == crcValue)
                        {
                            LogHelper.Log.Info("【标准帧】校验成功 " + sumCRC);
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
                            this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[4].Value = crcValue;
                            this.grid_stentSlowSignal.Rows[slowSignalCount].Cells[5].Value = sumCRCValue;
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
            //二进制位iData = Convert.ToString(packageInfo.Data[index], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 1], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 2], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 3], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 4], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 5], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 6], 2).PadLeft(4, '0') + " " + Convert.ToString(packageInfo.Data[index + 7], 2).PadLeft(4, '0');
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
            if (this.grid_stentCompleteSignal.RowCount < 1)
                return;
            var latestValue = this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[2].Value.ToString() + this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[3].Value.ToString() + this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[4].Value.ToString() + this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[5].Value.ToString() + this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[6].Value.ToString() + this.grid_stentCompleteSignal.Rows[this.grid_stentCompleteSignal.Rows.Count - 1].Cells[7].Value.ToString();
            byte[] latestByte = ConvertByte.HexToByte(latestValue);
            byte[] data1 = new byte[3];
            byte[] data2 = new byte[3];
            Array.Copy(latestByte,0,data1,0,3);
            Array.Copy(latestByte,3,data2,0,3);
            //数据1
            if (rb_highBefore1.CheckState == CheckState.Checked)
            {

            }
            else if(rb_lowerBefore1.CheckState == CheckState.Checked)
            {
                data1 = data1.Reverse().ToArray();
            }
            //数据2
            if (this.rb_highBefore2.CheckState == CheckState.Checked)
            {
            }
            else if (this.rb_lowerBefore2.CheckState == CheckState.Checked)
            {
                data2 = data2.Reverse().ToArray();
            }
            this.grid_stentQuickBoth.Invoke(new Action(()=>
            {
                //显示数据1与数据2
                if (this.grid_stentQuickBoth.Rows.Count < 1)
                    this.grid_stentQuickBoth.Rows.AddNew();
                this.grid_stentQuickBoth.Rows[0].Cells[0].Value = BitConverter.ToString(data1).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBoth.Rows[0].Cells[1].Value = BitConverter.ToString(data2).Replace("0", "").Replace("-", "");
                this.grid_stentQuickBoth.Rows[0].IsSelected = true;
                this.grid_stentQuickBoth.Update();
            }));
        }

        private void ConnectServerView()
        {
            AddConnection addConnection = new AddConnection();
            if (addConnection.ShowDialog() == DialogResult.OK)
            {
                if (!SuperEasyClient.client.IsConnected)
                {
                    MessageBox.Show("连接服务失败！","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }
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
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentCompleteSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentSlowSignal,false);
            RadGridViewProperties.SetRadGridViewProperty(this.grid_stentQuickBoth, false);
            this.grid_stentCompleteSignal.ReadOnly = true;
            this.grid_stentSlowSignal.ReadOnly = true;
            this.grid_stentQuickBoth.ReadOnly = true;
            this.rb_highBefore1.CheckState = CheckState.Checked;
            this.rb_highBefore2.CheckState = CheckState.Checked;
            cacheDataPerFrame = new List<string>();
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
        }

        private void SaveStentConfig()
        {
            var stentConfigPath = stentConfigDirectory + STENT_CONFIG_FILE;
            INIFile.SetValue(STENT_CONFIG_SECTION,STENT_CONFIG_FRAME_COUNT_KEY,cacheFrameNumber.ToString(),stentConfigPath);
        }
    }
}
