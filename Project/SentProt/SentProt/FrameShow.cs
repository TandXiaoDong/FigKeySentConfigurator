﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;

namespace SentProt
{
    public partial class FrameShow : Telerik.WinControls.UI.RadForm
    {
        public static int frameNumber = 10000;
        public FrameShow(int frame)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            frameNumber = frame;
            this.tb_frameNumber.Text = frameNumber.ToString();
            this.btn_apply.Click += Btn_apply_Click;
            this.btn_cancel.Click += Btn_cancel_Click;
        }

        private void Btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Btn_apply_Click(object sender, EventArgs e)
        {
            var frameValue = this.tb_frameNumber.Text;
            if (frameValue == "")
            {
                MessageBox.Show("显示帧数不能为空！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(frameValue, out frameNumber))
            {
                MessageBox.Show("请输入正整数的显示帧数！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                if (frameNumber <= 0)
                {
                    MessageBox.Show("请输入大于0的显示帧数！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
