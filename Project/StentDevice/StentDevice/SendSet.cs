﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace StentDevice
{
    public partial class SendSet : RadForm
    {
        public static int autoSendTimerInternal;
        public SendSet(int timerInternal)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            autoSendTimerInternal = timerInternal;
            this.tb_timerInternal.Text = autoSendTimerInternal.ToString();
            this.btn_apply.Click += Btn_apply_Click;
            this.btn_cancel.Click += Btn_cancel_Click;
        }

        private void Btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Btn_apply_Click(object sender, EventArgs e)
        {
            var timerInternal = this.tb_timerInternal.Text;
            if (timerInternal == "")
            {
                MessageBox.Show("发送时间间隔不能为空！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(timerInternal, out autoSendTimerInternal))
            {
                MessageBox.Show("请输入正整数的发送时间间隔！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (autoSendTimerInternal < 1)
            {
                MessageBox.Show("请输入大于0的时间间隔！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
