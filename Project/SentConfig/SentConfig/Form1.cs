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
        }

        private void Tool_channel1_Click(object sender, EventArgs e)
        {
            this.radDock1.AddDocument(this.dw_channel1);
            this.tool_channel3.Enabled = true;
            this.tool_channel2.Enabled = true;
            this.tool_channel1.Enabled = false;
        }

        private void SerialDev_RevSerialDataEvent(byte[] buffer)
        {
            
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
            ChannelEntity entity = new ChannelEntity();
            entity.ChannelIndex = 2;
            if (this.tg_send_ch2.Value)
            {
                entity.SendEnable = 1;
            }
            else
            {
                entity.SendEnable = 0;
            }

            if (this.tg_data_ch2.Value)
            {
                entity.DataSource = 0;
            }
            else
            {
                entity.DataSource = 1;
            }

            if (this.tg_CAN_ch2.Value)
            {
                entity.CANEnable = 1;
            }
            else
            {
                entity.CANEnable = 0;
            }
            entity.DAEnable = 0;

            if (!CheckDataValid(this.tb_f2_ch2))
                return;
            if (!CheckDataValid(this.tb_f2_ch2))
                return;
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

            if (this.tg_ldle_ch2.Value)
            {
                entity.IdleSt = 1;
            }
            else
            {
                entity.IdleSt = 0;
            }
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

            if (this.tg_data_ch1.Value)
            {
                entity.DataSource = 0;
            }
            else
            {
                entity.DataSource = 1;
            }

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
            }
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
            WriteLog(BitConverter.ToString(buffer));
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

        private void WriteLog(string content)
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
                    sw.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   " + content);
                }
            }
        }
    }
}
