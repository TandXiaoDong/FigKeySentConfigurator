namespace StentDevice
{
    partial class AddConnection
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddConnection));
            this.tb_hostname = new Telerik.WinControls.UI.RadTextBox();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.tb_port = new Telerik.WinControls.UI.RadTextBox();
            this.radLabel3 = new Telerik.WinControls.UI.RadLabel();
            this.materialTheme1 = new Telerik.WinControls.Themes.MaterialTheme();
            this.btn_connect = new Telerik.WinControls.UI.RadButton();
            this.breezeTheme1 = new Telerik.WinControls.Themes.BreezeTheme();
            this.btn_cancel = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.tb_hostname)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tb_port)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_connect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_cancel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // tb_hostname
            // 
            this.tb_hostname.BackColor = System.Drawing.Color.White;
            this.tb_hostname.Font = new System.Drawing.Font("华文中宋", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tb_hostname.Location = new System.Drawing.Point(48, 55);
            this.tb_hostname.Name = "tb_hostname";
            // 
            // 
            // 
            this.tb_hostname.RootElement.BorderHighlightColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tb_hostname.Size = new System.Drawing.Size(314, 38);
            this.tb_hostname.TabIndex = 4;
            this.tb_hostname.Text = "192.168.1.100";
            this.tb_hostname.ThemeName = "Material";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(48, 28);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(74, 21);
            this.radLabel2.TabIndex = 3;
            this.radLabel2.Text = "Hostname";
            this.radLabel2.ThemeName = "Material";
            // 
            // tb_port
            // 
            this.tb_port.BackColor = System.Drawing.Color.White;
            this.tb_port.Font = new System.Drawing.Font("华文中宋", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tb_port.Location = new System.Drawing.Point(48, 139);
            this.tb_port.Name = "tb_port";
            // 
            // 
            // 
            this.tb_port.RootElement.BorderHighlightColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tb_port.Size = new System.Drawing.Size(314, 38);
            this.tb_port.TabIndex = 7;
            this.tb_port.Text = "1001";
            this.tb_port.ThemeName = "Material";
            // 
            // radLabel3
            // 
            this.radLabel3.Location = new System.Drawing.Point(48, 111);
            this.radLabel3.Name = "radLabel3";
            this.radLabel3.Size = new System.Drawing.Size(34, 21);
            this.radLabel3.TabIndex = 6;
            this.radLabel3.Text = "Port";
            this.radLabel3.ThemeName = "Material";
            // 
            // btn_connect
            // 
            this.btn_connect.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_connect.Location = new System.Drawing.Point(48, 200);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(107, 33);
            this.btn_connect.TabIndex = 14;
            this.btn_connect.Text = "开始连接";
            this.btn_connect.ThemeName = "Breeze";
            // 
            // btn_cancel
            // 
            this.btn_cancel.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_cancel.Location = new System.Drawing.Point(251, 200);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(111, 33);
            this.btn_cancel.TabIndex = 16;
            this.btn_cancel.Text = "取消连接";
            this.btn_cancel.ThemeName = "Breeze";
            // 
            // AddConnection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Lavender;
            this.ClientSize = new System.Drawing.Size(383, 253);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_connect);
            this.Controls.Add(this.tb_port);
            this.Controls.Add(this.radLabel3);
            this.Controls.Add(this.tb_hostname);
            this.Controls.Add(this.radLabel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AddConnection";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "AddConnection";
            this.ThemeName = "Material";
            ((System.ComponentModel.ISupportInitialize)(this.tb_hostname)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tb_port)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_connect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btn_cancel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Telerik.WinControls.UI.RadTextBox tb_hostname;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadTextBox tb_port;
        private Telerik.WinControls.UI.RadLabel radLabel3;
        private Telerik.WinControls.Themes.MaterialTheme materialTheme1;
        private Telerik.WinControls.UI.RadButton btn_connect;
        private Telerik.WinControls.Themes.BreezeTheme breezeTheme1;
        private Telerik.WinControls.UI.RadButton btn_cancel;
    }
}
