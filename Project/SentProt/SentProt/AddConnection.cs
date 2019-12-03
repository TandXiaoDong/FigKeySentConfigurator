using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using SentProt.ClientSocket;
using SentProt.ClientSocket.AppBase;
using CommonUtils.ByteHelper;

namespace SentProt
{
    public partial class AddConnection : Telerik.WinControls.UI.RadForm
    {
        private delegate void Actions();
        private Actions actions;
        public AddConnection()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            Init();
            EventHandlers();
        }

        private void Init()
        {
            this.tb_hostname.Text = "127.0.0.1";
            this.tb_port.Text = "10010";
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
            this.Close();
        }

        private void SendMessage()
        {
            //SuperEasyClient.SendMessage(StentSignalEnum.RequestData, new byte[0]);
        }

        private void Btn_connect_Click(object sender, EventArgs e)
        {
            SuperEasyClient.serverUrl = this.tb_hostname.Text;
            SuperEasyClient.serverPort = this.tb_port.Text;
            SuperEasyClient.ConnectServer();
        }

        private void Btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddConnection_Load(object sender, EventArgs e)
        {
            
        }
    }
}
