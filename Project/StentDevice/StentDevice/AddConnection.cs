using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using StentDevice.ClientSocket;
using StentDevice.ClientSocket.AppBase;
using CommonUtils.ByteHelper;
using System.Threading.Tasks;

namespace StentDevice
{
    public partial class AddConnection : Telerik.WinControls.UI.RadForm
    {
        private delegate void Actions();
        private Actions actions;
        private bool IsConnect;
        public static string serverIP;
        public static int serverPort;
        public AddConnection(string ip,int port)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            serverIP = ip;
            serverPort = port;
            EventHandlers();
        }

        private void EventHandlers()
        {
            this.Load += AddConnection_Load;
            this.btn_cancel.Click += Btn_cancel_Click;
            this.btn_connect.Click += Btn_connect_Click;
            SuperEasyClient.NoticeConnectEvent += SuperEasyClient_NoticeConnectEvent;
            actions = new Actions(RefreshControls);
        }

        private void SuperEasyClient_NoticeConnectEvent(bool IsConnect)
        {
            if (IsConnect)
            {
                if(this.InvokeRequired)
                    this.Invoke(actions);
            }
        }

        private void RefreshControls()
        {
            SendMessage();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SendMessage()
        {
            //SuperEasyClient.SendMessage(StentSignalEnum.RequestData, new byte[0]);
        }

        private void Btn_connect_Click(object sender, EventArgs e)
        {
            if (this.tb_hostname.Text != "")
                serverIP = this.tb_hostname.Text;
            else
            {
                MessageBox.Show("请输入服务器地址！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.tb_hostname.Focus();
                return;
            }
            if (this.tb_port.Text != "")
                int.TryParse(this.tb_port.Text.Trim(), out serverPort);
            else
            {
                MessageBox.Show("请输入正确的端口号！","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                this.tb_port.Focus();
                return;
            }
            SuperEasyClient.serverUrl = serverIP;
            SuperEasyClient.serverPort = serverPort.ToString();
            SuperEasyClient.ConnectServer();
            if (!IsConnect)
            {
                this.btn_connect.Text = "正在连接...";
                this.btn_connect.BackColor = Color.Gray;
                IsConnect = !IsConnect;
            }
            else
            {
                this.btn_connect.Text = "开始连接";
                this.btn_connect.BackColor = Color.RoyalBlue;
                IsConnect = !IsConnect;
            }
        }

        private void Btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddConnection_Load(object sender, EventArgs e)
        {
            if (serverIP != "")
                this.tb_hostname.Text = serverIP;
            else
                this.tb_hostname.Text = "127.0.0.1";
            if (serverPort != 0)
                this.tb_port.Text = serverPort.ToString();
            else
                this.tb_port.Text = "1001";
        }
    }
}
