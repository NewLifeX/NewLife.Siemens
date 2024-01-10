namespace TestClient
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            tb_address = new TextBox();
            tb_port = new TextBox();
            btn_conn = new Button();
            rtb_content = new RichTextBox();
            btn_write = new Button();
            tb_pointAddress = new TextBox();
            tb_value = new TextBox();
            label3 = new Label();
            label4 = new Label();
            btn_read = new Button();
            label5 = new Label();
            label6 = new Label();
            tb_type = new TextBox();
            tb_length = new TextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 24);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(54, 20);
            label1.TabIndex = 0;
            label1.Text = "地址：";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(246, 24);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(54, 20);
            label2.TabIndex = 1;
            label2.Text = "端口：";
            // 
            // tb_address
            // 
            tb_address.Location = new Point(70, 22);
            tb_address.Margin = new Padding(2);
            tb_address.Name = "tb_address";
            tb_address.Size = new Size(172, 27);
            tb_address.TabIndex = 2;
            tb_address.Text = "127.0.0.1";
            // 
            // tb_port
            // 
            tb_port.Location = new Point(303, 22);
            tb_port.Margin = new Padding(2);
            tb_port.Name = "tb_port";
            tb_port.Size = new Size(151, 27);
            tb_port.TabIndex = 3;
            tb_port.Text = "102";
            // 
            // btn_conn
            // 
            btn_conn.Location = new Point(468, 20);
            btn_conn.Margin = new Padding(2);
            btn_conn.Name = "btn_conn";
            btn_conn.Size = new Size(92, 28);
            btn_conn.TabIndex = 4;
            btn_conn.Text = "连接";
            btn_conn.UseVisualStyleBackColor = true;
            btn_conn.Click += button1_Click;
            // 
            // rtb_content
            // 
            rtb_content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtb_content.Location = new Point(10, 144);
            rtb_content.Margin = new Padding(2);
            rtb_content.Name = "rtb_content";
            rtb_content.Size = new Size(762, 342);
            rtb_content.TabIndex = 5;
            rtb_content.Text = "";
            // 
            // btn_write
            // 
            btn_write.Location = new Point(468, 61);
            btn_write.Margin = new Padding(2);
            btn_write.Name = "btn_write";
            btn_write.Size = new Size(92, 28);
            btn_write.TabIndex = 6;
            btn_write.Text = "写入";
            btn_write.UseVisualStyleBackColor = true;
            btn_write.Click += btn_write_Click;
            // 
            // tb_pointAddress
            // 
            tb_pointAddress.Location = new Point(70, 62);
            tb_pointAddress.Margin = new Padding(2);
            tb_pointAddress.Name = "tb_pointAddress";
            tb_pointAddress.Size = new Size(172, 27);
            tb_pointAddress.TabIndex = 7;
            tb_pointAddress.Text = "DB1.DBD32";
            // 
            // tb_value
            // 
            tb_value.Location = new Point(303, 62);
            tb_value.Margin = new Padding(2);
            tb_value.Name = "tb_value";
            tb_value.Size = new Size(151, 27);
            tb_value.TabIndex = 8;
            tb_value.Text = "5";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(14, 65);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(54, 20);
            label3.TabIndex = 9;
            label3.Text = "点位：";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(261, 65);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(39, 20);
            label4.TabIndex = 10;
            label4.Text = "值：";
            // 
            // btn_read
            // 
            btn_read.Location = new Point(578, 61);
            btn_read.Margin = new Padding(2);
            btn_read.Name = "btn_read";
            btn_read.Size = new Size(92, 28);
            btn_read.TabIndex = 11;
            btn_read.Text = "读取";
            btn_read.UseVisualStyleBackColor = true;
            btn_read.Click += btn_read_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(14, 106);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(54, 20);
            label5.TabIndex = 12;
            label5.Text = "类型：";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(246, 106);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(54, 20);
            label6.TabIndex = 13;
            label6.Text = "长度：";
            // 
            // tb_type
            // 
            tb_type.Location = new Point(70, 103);
            tb_type.Margin = new Padding(2);
            tb_type.Name = "tb_type";
            tb_type.Size = new Size(172, 27);
            tb_type.TabIndex = 14;
            tb_type.Text = "int";
            // 
            // tb_length
            // 
            tb_length.Location = new Point(303, 103);
            tb_length.Margin = new Padding(2);
            tb_length.Name = "tb_length";
            tb_length.Size = new Size(151, 27);
            tb_length.TabIndex = 15;
            tb_length.Text = "2";
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(781, 495);
            Controls.Add(tb_length);
            Controls.Add(tb_type);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(btn_read);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(tb_value);
            Controls.Add(tb_pointAddress);
            Controls.Add(btn_write);
            Controls.Add(rtb_content);
            Controls.Add(btn_conn);
            Controls.Add(tb_port);
            Controls.Add(tb_address);
            Controls.Add(label2);
            Controls.Add(label1);
            Margin = new Padding(2);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "S7测试";
            Load += FrmMain_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox tb_address;
        private TextBox tb_port;
        private Button btn_conn;
        private RichTextBox rtb_content;
        private Button btn_write;
        private TextBox tb_pointAddress;
        private TextBox tb_value;
        private Label label3;
        private Label label4;
        private Button btn_read;
        private Label label5;
        private Label label6;
        private TextBox tb_type;
        private TextBox tb_length;
    }
}
