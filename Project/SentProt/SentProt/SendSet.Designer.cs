namespace SentProt
{
    partial class SendSet
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SendSet));
            this.materialTheme1 = new Telerik.WinControls.Themes.MaterialTheme();
            this.btn_cancel = new Telerik.WinControls.UI.RadButton();
            this.btn_apply = new Telerik.WinControls.UI.RadButton();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.tb_timerInternal = new Telerik.WinControls.UI.RadTextBox();
            this.cb_auto = new Telerik.WinControls.UI.RadCheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.btn_cancel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_apply)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tb_timerInternal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_auto)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_cancel
            // 
            this.btn_cancel.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_cancel.Location = new System.Drawing.Point(180, 141);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(101, 33);
            this.btn_cancel.TabIndex = 22;
            this.btn_cancel.Text = "取消";
            this.btn_cancel.ThemeName = "Breeze";
            // 
            // btn_apply
            // 
            this.btn_apply.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_apply.Location = new System.Drawing.Point(42, 141);
            this.btn_apply.Name = "btn_apply";
            this.btn_apply.Size = new System.Drawing.Size(107, 33);
            this.btn_apply.TabIndex = 21;
            this.btn_apply.Text = "应用";
            this.btn_apply.ThemeName = "Breeze";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(42, 30);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(102, 21);
            this.radLabel2.TabIndex = 17;
            this.radLabel2.Text = "发送时间间隔";
            this.radLabel2.ThemeName = "Material";
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(287, 72);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(39, 21);
            this.radLabel1.TabIndex = 23;
            this.radLabel1.Text = "毫秒";
            this.radLabel1.ThemeName = "Material";
            // 
            // tb_timerInternal
            // 
            this.tb_timerInternal.Location = new System.Drawing.Point(42, 57);
            this.tb_timerInternal.Name = "tb_timerInternal";
            this.tb_timerInternal.Size = new System.Drawing.Size(239, 36);
            this.tb_timerInternal.TabIndex = 24;
            this.tb_timerInternal.Text = "3000";
            this.tb_timerInternal.ThemeName = "Material";
            // 
            // cb_auto
            // 
            this.cb_auto.Location = new System.Drawing.Point(42, 99);
            this.cb_auto.Name = "cb_auto";
            this.cb_auto.Size = new System.Drawing.Size(57, 19);
            this.cb_auto.TabIndex = 25;
            this.cb_auto.Text = "启用";
            this.cb_auto.ThemeName = "Material";
            // 
            // SendSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Lavender;
            this.ClientSize = new System.Drawing.Size(332, 186);
            this.Controls.Add(this.cb_auto);
            this.Controls.Add(this.tb_timerInternal);
            this.Controls.Add(this.radLabel1);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_apply);
            this.Controls.Add(this.radLabel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SendSet";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "发送设置";
            this.ThemeName = "Material";
            ((System.ComponentModel.ISupportInitialize)(this.btn_cancel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_apply)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tb_timerInternal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_auto)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Telerik.WinControls.Themes.MaterialTheme materialTheme1;
        private Telerik.WinControls.UI.RadButton btn_cancel;
        private Telerik.WinControls.UI.RadButton btn_apply;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadTextBox tb_timerInternal;
        private Telerik.WinControls.UI.RadCheckBox cb_auto;
    }
}
