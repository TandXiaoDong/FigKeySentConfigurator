using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using System.IO;

namespace SentConfig
{
    public partial class Form1 : Form
    {
        private SerialPortDevice serialDev;
        private Queue<string[]> autoSendQueue;
        private MTTimer mtTimer;
        private bool IsStartRead;
        private DataTable fileData;
        private bool IsStopSend;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.radDock1.DocumentTabsVisible = false;
            this.radDock1.RemoveAllDocumentWindows();
            //this.radDock1.AddDocument(this.dw_channel3);
            this.radDock1.AddDocument(this.dw_channel2);
            this.radDock1.AddDocument(this.dw_channel1);
            this.radDock1.ActiveWindow = this.dw_channel1;
            this.tool_channel1.Enabled = false;
            this.serialDev = new SerialPortDevice();
            this.autoSendQueue = new Queue<string[]>();
            SearchSerial();

            this.tool_close.Click += Tool_close_Click;
            this.tool_open.Click += Tool_open_Click;
            this.tool_refresh.Click += Tool_refresh_Click;
            this.btn_cfg_ch1.Click += Btn_cfg_ch1_Click;
            this.btn_cfg_ch2.Click += Btn_cfg_ch2_Click;
            this.btn_cfg_ch3.Click += Btn_cfg_ch3_Click;
            this.tool_channel1.Click += Tool_channel1_Click;
            this.tool_channel2.Click += Tool_channel2_Click;
            this.tool_channel3.Click += Tool_channel3_Click;
            this.tool_abort.Click += Tool_abort_Click;
            this.serialDev.RevSerialDataEvent += SerialDev_RevSerialDataEvent;
            this.tool_excel.Click += Tool_excel_Click;
            this.btn_autoSendCh1.Click += Btn_autoSendCh1_Click;
            this.btn_readFdataCh1.Click += Btn_readFdataCh1_Click;
            this.btn_readFdataCh2.Click += Btn_readFdataCh2_Click;
            this.tool_stopSend.Click += Tool_stopSend_Click;
            this.FormClosed += Form1_FormClosed;
        }

        private void Tool_stopSend_Click(object sender, EventArgs e)
        {
            this.IsStopSend = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }

        private void MtTimerRun(object sender, long JumpPeriod, long interval)
        {
            AutoSendCh1Command();
        }
        private void MtTimerOpen()
        {
            this.mtTimer = new MTTimer(100, 1000 * 10, 1, MtTimerRun);
            this.mtTimer.Open();
        }
        private void MtTimerClose()
        {
            this.mtTimer.Dispose();
        }

        private void Btn_readFdataCh1_Click(object sender, EventArgs e)
        {
            //55-AA-01-00-23-01-67-05-91
            this.IsStartRead = true;
            SendReadCh1Command(0, 0, 1);
        }

        private void Btn_readFdataCh2_Click(object sender, EventArgs e)
        {
            this.IsStartRead = true;
            SendCh2Command(0, 0, 1);
        }

        private void Btn_autoSendCh1_Click(object sender, EventArgs e)
        {
            var fileObj = FileSelect.GetSelectFileContent("(*.xls)|*.xls|(*.xlsx)|*.xlsx", "选择表格");
            if (fileObj == null)
                return;
            if (fileObj.FileName == "")
                return;
            DataTable data = ExcelHelper.ExcelToDataTable("", true, fileObj.FileName);

            if (data.Rows.Count <= 0)
            {
                MessageBox.Show("表格种没有查询到数据！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.IsStopSend = false;
            this.fileData = data;
            AddFileData2Queue(this.fileData);
            EnableControlCh1(false);
            MtTimerOpen();
        }

        private void EnableControlCh1(bool enable)
        {
            this.tool_channel2.Enabled = enable;
            this.tool_excel.Enabled = enable;
            this.btn_readFdataCh1.Enabled = enable;
            this.btn_autoSendCh1.Enabled = enable;
            this.btn_cfg_ch1.Enabled = enable;

            this.tg_send_ch1.Enabled = enable;
            this.tg_data_ch1.Enabled = enable;
            this.tg_CAN_ch1.Enabled = enable;
            this.tg_DA_ch1.Enabled = enable;
            this.tg_sync_ch1.Enabled = enable;
            this.tg_crc_ch1.Enabled = enable;
            this.tg_tick_ch1.Enabled = enable;
            this.tg_ldle_ch1.Enabled = enable;
            this.tg_exchange_ch1.Enabled = enable;
        }

        private void AddFileData2Queue(DataTable data)
        {
            this.autoSendQueue.Clear();
            foreach (DataRow dr in data.Rows)
            {
                this.autoSendQueue.Enqueue(new string[] { dr[0].ToString(), dr[1].ToString(), "0" });
            }
        }

        private void AutoSendCh1Command()
        {
            if (this.IsStopSend)
            {
                EnableControlCh1(true);
                MtTimerClose();
            }
            if (this.autoSendQueue.Count > 0)
            {
                string[] data = this.autoSendQueue.Dequeue();
                if (!SendCh1Command(data[0], data[1], int.Parse(data[2])))//发送失败，停止发送
                {
                    this.btn_autoSendCh1.Enabled = true;
                    this.btn_cfg_ch1.Enabled = true;
                    MtTimerClose();
                }
            }
            else//发送完成，继续下一个循环发送
            {
                if(!this.IsStopSend)
                {
                    AddFileData2Queue(this.fileData);
                    AutoSendCh1Command();
                }
            }
        }

        private void Tool_excel_Click(object sender, EventArgs e)
        {
            this.tool_excel.Enabled = false;
            DataTable data = new DataTable();
            data.Columns.Add("f1(hex)");
            data.Columns.Add("f2(hex)");
            for(int i = 0;i < 10;i++)
            {
                DataRow dr = data.NewRow();
                dr[0] = i;
                dr[1] = i;
                data.Rows.Add(dr);
            }
            var fileName = AppDomain.CurrentDomain.BaseDirectory + "data\\";
            if (!Directory.Exists(fileName))
                Directory.CreateDirectory(fileName);
            if (File.Exists(fileName + "fdata.xls"))
            {
                File.Delete(fileName + "fdata.xls");
            }
            ExcelHelper.DataTableToExcel(data, "sheet1", true, fileName + "fdata.xls", false);
            System.Diagnostics.Process.Start(fileName);
            this.tool_excel.Enabled = true;
        }

        private void Tool_abort_Click(object sender, EventArgs e)
        {
            Helper helper = new Helper();
            helper.Show();
        }

        private void Tool_channel3_Click(object sender, EventArgs e)
        {
            this.radDock1.AddDocument(this.dw_channel3);
            this.tool_channel3.Enabled = false;
            this.tool_channel2.Enabled = true;
            this.tool_channel1.Enabled = true;
        }

        private void Tool_channel2_Click(object sender, EventArgs e)
        {
            this.radDock1.AddDocument(this.dw_channel2);
            this.tool_channel3.Enabled = true;
            this.tool_channel2.Enabled = false;
            this.tool_channel1.Enabled = true;
            this.tool_stopSend.Enabled = false;
        }

        private void Tool_channel1_Click(object sender, EventArgs e)
        {
            this.radDock1.AddDocument(this.dw_channel1);
            this.tool_channel3.Enabled = true;
            this.tool_channel2.Enabled = true;
            this.tool_channel1.Enabled = false;
            this.tool_stopSend.Enabled = true;
        }

        private void SerialDev_RevSerialDataEvent(byte[] buffer)
        {
            if (buffer.Length <= 0)
                return;
            //显示通道1的 f1/f2
            if (this.IsStartRead)
            {
                this.IsStartRead = false;
                this.Invoke(new Action(() =>
                {
                    if (buffer[2] == 0x01)
                    {
                        this.tb_f1_ch1.Text = buffer[5].ToString() + buffer[4].ToString();
                        this.tb_f2_ch1.Text = buffer[7].ToString() + buffer[6].ToString();
                    }
                    else if (buffer[2] == 0x02)
                    {
                        this.tb_f1_ch2.Text = buffer[5].ToString() + buffer[4].ToString();
                        this.tb_f2_ch2.Text = buffer[7].ToString() + buffer[6].ToString();
                    }
                }));
            }
        }

        private void Btn_cfg_ch3_Click(object sender, EventArgs e)
        {
            ChannelEntity entity = new ChannelEntity();
            entity.ChannelIndex = 2;
            if (this.tg_send_ch3.Value)
            {
                entity.SendEnable = 1;
            }
            else
            {
                entity.SendEnable = 0;
            }

            if (this.tg_data_ch3.Value)
            {
                entity.DataSource = 0;
            }
            else
            {
                entity.DataSource = 1;
            }

            if (this.tg_CAN_ch3.Value)
            {
                entity.CANEnable = 1;
            }
            else
            {
                entity.CANEnable = 0;
            }

            if (this.tg_DA_ch3.Value)
            {
                entity.DAEnable = 1;
            }
            else
            {
                entity.DAEnable = 0;
            }

            if (!CheckDataValid(this.tb_f1_ch3))
                return;
            if (!CheckDataValid(this.tb_f2_ch3))
                return;
            var f1 = this.tb_f1_ch3.Text.Trim().ToLower().Replace("0x", "");
            var f2 = this.tb_f2_ch3.Text.Trim().ToLower().Replace("0x", "");
            f1 = f1.PadLeft(4, '0');
            f2 = f2.PadLeft(4, '0');
            //entity.UsartF1 = f1.Substring(2, 2) + f1.Substring(0, 2);
            //entity.UsartF2 = f2.Substring(2, 2) + f2.Substring(0, 2);
            entity.UsartF1 = f1;
            entity.UsartF2 = f2;
            if (this.tg_sync_ch3.Value)
            {
                entity.SyncErr = 1;
            }
            else
            {
                entity.SyncErr = 0;
            }

            if (this.tg_crc_ch3.Value)
            {
                entity.CrcErr = 1;
            }
            else
            {
                entity.CrcErr = 0;
            }

            if (this.tg_tick_ch3.Value)
            {
                entity.UsTick = 1;
            }
            else
            {
                entity.UsTick = 0;
            }

            if (this.tg_ldle_ch3.Value)
            {
                entity.IdleSt = 1;
            }
            else
            {
                entity.IdleSt = 0;
            }
            if (this.tg_exchange_ch3.Value)
            {
                entity.ChangeF2 = 1;
            }
            else
            {
                entity.ChangeF2 = 0;
            }
            SendConfig(entity);
        }

        private void Btn_cfg_ch2_Click(object sender, EventArgs e)
        {
            if (!CheckDataValid(this.tb_f2_ch2))
                return;
            if (!CheckDataValid(this.tb_f2_ch2))
                return;
            int sendEnable = 0;
            if (this.tg_send_ch2.Value)
            {
                sendEnable = 1;
            }
            else
            {
                sendEnable = 0;
            }

            int datasource = 0;
            if (this.tg_data_ch2.Value)
            {
                datasource = 0;
            }
            else
            {
                datasource = 1;
            }
            int idst = 0;
            if (this.tg_ldle_ch2.Value)
            {
                idst = 1;
            }
            else
            {
                idst = 0;
            }
            SendCh2Command(sendEnable, datasource, idst);
        }

        private void SendCh2Command(int sendEnable, int datasource, int idleSt)
        {
            ChannelEntity entity = new ChannelEntity();
            entity.ChannelIndex = 2;

            entity.SendEnable = sendEnable;
            entity.DataSource = datasource;

            if (this.tg_CAN_ch2.Value)
            {
                entity.CANEnable = 1;
            }
            else
            {
                entity.CANEnable = 0;
            }
            entity.DAEnable = 0;

            var f1 = this.tb_f1_ch2.Text.Trim().ToLower().Replace("0x", "");
            var f2 = this.tb_f2_ch2.Text.Trim().ToLower().Replace("0x", "");
            f1 = f1.PadLeft(4, '0');
            f2 = f2.PadLeft(4, '0');
            //entity.UsartF1 = f1.Substring(2, 2) + f1.Substring(0, 2);
            //entity.UsartF2 = f2.Substring(2, 2) + f2.Substring(0, 2);
            entity.UsartF1 = f1;
            entity.UsartF2 = f2;

            if (this.tg_sync_ch2.Value)
            {
                entity.SyncErr = 1;
            }
            else
            {
                entity.SyncErr = 0;
            }

            if (this.tg_crc_ch2.Value)
            {
                entity.CrcErr = 1;
            }
            else
            {
                entity.CrcErr = 0;
            }

            if (this.tg_tick_ch2.Value)
            {
                entity.UsTick = 1;
            }
            else
            {
                entity.UsTick = 0;
            }

            entity.IdleSt = idleSt;

            if (this.tg_exchange_ch2.Value)
            {
                entity.ChangeF2 = 1;
            }
            else
            {
                entity.ChangeF2 = 0;
            }
            if (!SendConfig(entity))
            {
                MessageBox.Show("发送失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Btn_cfg_ch1_Click(object sender, EventArgs e)
        {
            if (!CheckDataValid(this.tb_f1_ch1))
                return;
            if (!CheckDataValid(this.tb_f2_ch1))
                return;
            var f1 = this.tb_f1_ch1.Text.Trim().ToLower().Replace("0x", "");
            var f2 = this.tb_f2_ch1.Text.Trim().ToLower().Replace("0x", "");
            f1 = f1.PadLeft(4, '0');
            f2 = f2.PadLeft(4, '0');
            //entity.UsartF1 = f1.Substring(2, 2) + f1.Substring(0, 2);
            //entity.UsartF2 = f2.Substring(2, 2) + f2.Substring(0, 2);

            int dataSource = 0;
            if (this.tg_data_ch1.Value)
            {
                dataSource = 0;
            }
            else
            {
                dataSource = 1;
            }
            SendCh1Command(f1, f2, dataSource);
        }

        private bool SendCh1Command(string f1, string f2, int dataSource)
        {
            ChannelEntity entity = new ChannelEntity();
            entity.ChannelIndex = 1;
            if (this.tg_send_ch1.Value)
            {
                entity.SendEnable = 1;
            }
            else
            {
                entity.SendEnable = 0;
            }

            entity.DataSource = dataSource;

            if (this.tg_CAN_ch1.Value)
            {
                entity.CANEnable = 1;
            }
            else
            {
                entity.CANEnable = 0;
            }

            if (this.tg_DA_ch1.Value)
            {
                entity.DAEnable = 1;
            }
            else
            {
                entity.DAEnable = 0;
            }

            entity.UsartF1 = f1;
            entity.UsartF2 = f2;

            if (this.tg_sync_ch1.Value)
            {
                entity.SyncErr = 1;
            }
            else
            {
                entity.SyncErr = 0;
            }

            if (this.tg_crc_ch1.Value)
            {
                entity.CrcErr = 1;
            }
            else
            {
                entity.CrcErr = 0;
            }

            if (this.tg_tick_ch1.Value)
            {
                entity.UsTick = 1;
            }
            else
            {
                entity.UsTick = 0;
            }

            if (this.tg_ldle_ch1.Value)
            {
                entity.IdleSt = 1;
            }
            else
            {
                entity.IdleSt = 0;
            }
            if (this.tg_exchange_ch1.Value)
            {
                entity.ChangeF2 = 1;
            }
            else
            {
                entity.ChangeF2 = 0;
            }
            if (!SendConfig(entity))
            {
                MessageBox.Show("发送失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool SendReadCh1Command(int sendEnable, int datasource, int idleSt)
        {
            ChannelEntity entity = new ChannelEntity();
            entity.ChannelIndex = 1;
            //if (this.tg_send_ch1.Value)
            //{
            //    entity.SendEnable = 1;
            //}
            //else
            //{
            //    entity.SendEnable = 0;
            //}
            entity.SendEnable = sendEnable;

            if (this.tg_CAN_ch1.Value)
            {
                entity.CANEnable = 1;
            }
            else
            {
                entity.CANEnable = 0;
            }

            if (this.tg_DA_ch1.Value)
            {
                entity.DAEnable = 1;
            }
            else
            {
                entity.DAEnable = 0;
            }

            //if (this.tg_data_ch1.Value)
            //{
            //    entity.DataSource = 0;
            //}
            //else
            //{
            //    entity.DataSource = 1;
            //}

            entity.DataSource = datasource;

            var f1 = this.tb_f1_ch1.Text.Trim().ToLower().Replace("0x", "");
            var f2 = this.tb_f2_ch1.Text.Trim().ToLower().Replace("0x", "");
            f1 = f1.PadLeft(4, '0');
            f2 = f2.PadLeft(4, '0');
            //entity.UsartF1 = f1.Substring(2, 2) + f1.Substring(0, 2);
            //entity.UsartF2 = f2.Substring(2, 2) + f2.Substring(0, 2);

            entity.UsartF1 = f1;
            entity.UsartF2 = f2;

            if (this.tg_sync_ch1.Value)
            {
                entity.SyncErr = 1;
            }
            else
            {
                entity.SyncErr = 0;
            }

            if (this.tg_crc_ch1.Value)
            {
                entity.CrcErr = 1;
            }
            else
            {
                entity.CrcErr = 0;
            }

            if (this.tg_tick_ch1.Value)
            {
                entity.UsTick = 1;
            }
            else
            {
                entity.UsTick = 0;
            }

            //if (this.tg_ldle_ch1.Value)
            //{
            //    entity.IdleSt = 1;
            //}
            //else
            //{
            //    entity.IdleSt = 0;
            //}

            entity.IdleSt = idleSt;

            if (this.tg_exchange_ch1.Value)
            {
                entity.ChangeF2 = 1;
            }
            else
            {
                entity.ChangeF2 = 0;
            }
            if (!SendConfig(entity))
            {
                MessageBox.Show("发送失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool CheckDataValid(RadTextBox text)
        {
            var str = text.Text.Trim();
            try
            {
                var decVal = Convert.ToInt32(str, 16);
                if (decVal > 0xffff || decVal < 0)
                {
                    text.Focus();
                    MessageBox.Show("超出范围！", "Err", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                text.Focus();
                MessageBox.Show("格式错误！" + ex.Message, "Err", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Tool_refresh_Click(object sender, EventArgs e)
        {
            SearchSerial();
        }

        private void Tool_open_Click(object sender, EventArgs e)
        {
            if (this.serialDev == null)
            {
                this.serialDev = new SerialPortDevice();
            }
           var openStatus = this.serialDev.OpenSerialPort(this.tool_serialNames.Text.Trim());
            if (openStatus)
            {
                this.tool_open.Enabled = false;
                this.tool_close.Enabled = true;
                this.btn_cfg_ch1.Enabled = true;
                this.btn_cfg_ch2.Enabled = true;
            }
            else
            {
                this.btn_cfg_ch1.Enabled = false;
                this.btn_cfg_ch2.Enabled = false;
            }
        }

        private void Tool_close_Click(object sender, EventArgs e)
        {
            if (this.serialDev.CloseSerialPort())
            {
                this.tool_open.Enabled = true;
                this.tool_close.Enabled = false;
                this.btn_cfg_ch1.Enabled = false;
                this.btn_cfg_ch2.Enabled = false;
            }
            else
            {
                this.btn_cfg_ch1.Enabled = true;
                this.btn_cfg_ch2.Enabled = true;
            }
        }

        private bool SendConfig(ChannelEntity entity)
        {
            byte[] buffer = new byte[9];
            buffer[0] = 0x55;
            buffer[1] = 0xaa;
            string _4bitCh = ReverString(Convert.ToString(entity.ChannelIndex, 2).PadLeft(4, '0'));
            string byte2 = _4bitCh + entity.DataSource.ToString() +  entity.CANEnable.ToString() + entity.DAEnable +"0";
            byte2 = ReverString(byte2);
            buffer[2] = Convert.ToByte(byte2, 2);
            string byte3 = "00" + entity.ChangeF2.ToString() + entity.IdleSt.ToString() + 
                entity.UsTick.ToString() + entity.CrcErr.ToString() + entity.SyncErr.ToString() + 
                entity.SendEnable.ToString();
            byte3 = ReverString(byte3);
            buffer[3] = Convert.ToByte(byte3, 2);
            //var bitF1 = Convert.ToString(Convert.ToInt32(entity.UsartF1, 16), 2).PadLeft(16, '0');
            //bitF1 = Convert.ToString(Convert.ToInt32(bitF1.Substring(4), 2), 16).PadLeft(4, '0');
            var bitF1 = Convert.ToInt32(entity.UsartF1, 16);
            buffer[4] = (byte)(bitF1 & 0xff);
            buffer[5] = (byte)((bitF1 >> 8) & 0xff);

            var bitF2 = Convert.ToInt32(entity.UsartF2, 16);
            buffer[6] = (byte)(bitF2 & 0xff);
            buffer[7] = (byte)((bitF2 >> 8) & 0xff);
            //buffer[4] = Convert.ToByte(bitF1.Substring(0, 2), 16);
            //buffer[5] = Convert.ToByte(bitF1.Substring(0, 2), 16);
            //var bitF2 = Convert.ToString(Convert.ToInt32(entity.UsartF2, 16), 2).PadLeft(16, '0');
            //bitF2 = Convert.ToString(Convert.ToInt32(bitF2.Substring(4), 2), 16).PadLeft(4, '0');
            //buffer[6] = Convert.ToByte(bitF2.Substring(0, 2), 16);
            //buffer[7] = Convert.ToByte(bitF2.Substring(0, 2), 16);
            var crc = (buffer[2] + buffer[3] + buffer[4] + buffer[5] + buffer[6] + buffer[7]) & 0xff;
            buffer[8] = (byte)crc; 
            if (this.serialDev == null)
                return false;
            return this.serialDev.WriteSerialPort(buffer);
        }

        private string ReverString(string bitstr)
        {
            var chArr = bitstr.ToCharArray();
            string str = "";
            for (int i = chArr.Length - 1; i >= 0; i--)
            {
                str += chArr[i];
            }
            return str;
        }

        private void SearchSerial()
        {
            var comNames = System.IO.Ports.SerialPort.GetPortNames();
            if (comNames.Length > 0)
            {
                this.tool_serialNames.Items.Clear();
                this.tool_serialNames.Items.AddRange(comNames);
                this.tool_serialNames.SelectedIndex = 0;
                this.tool_open.Enabled = true;
                this.tool_close.Enabled = false;
            }
        }

        private void WriteLogs(string content)
        {
            var dirPath = AppDomain.CurrentDomain.BaseDirectory + "Log\\";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            using (FileStream fs = new FileStream(dirPath + System.DateTime.Now.ToString("yyyyMMddHH") + ".txt", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "   " + content);
                }
            }
        }
    }
}
