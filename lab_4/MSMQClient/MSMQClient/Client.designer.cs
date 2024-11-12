namespace MSMQ
{
    partial class frmMain
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSend = new System.Windows.Forms.Button();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.lblIP = new System.Windows.Forms.Label();
            this.btnConnectDisconnet = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.tbPath = new System.Windows.Forms.TextBox();
            this.tbUserName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rtbMessages = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(425, 131);
            this.btnSend.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(136, 40);
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "Отправить";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // tbMessage
            // 
            this.tbMessage.Location = new System.Drawing.Point(141, 137);
            this.tbMessage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.Size = new System.Drawing.Size(270, 26);
            this.tbMessage.TabIndex = 1;
            // 
            // lblIP
            // 
            this.lblIP.AutoSize = true;
            this.lblIP.Location = new System.Drawing.Point(13, 83);
            this.lblIP.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblIP.Name = "lblIP";
            this.lblIP.Size = new System.Drawing.Size(127, 20);
            this.lblIP.TabIndex = 2;
            this.lblIP.Text = "Путь к очереди";
            // 
            // btnConnectDisconnet
            // 
            this.btnConnectDisconnet.Location = new System.Drawing.Point(425, 72);
            this.btnConnectDisconnet.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnConnectDisconnet.Name = "btnConnectDisconnet";
            this.btnConnectDisconnet.Size = new System.Drawing.Size(136, 40);
            this.btnConnectDisconnet.TabIndex = 2;
            this.btnConnectDisconnet.Text = "Подключиться";
            this.btnConnectDisconnet.UseVisualStyleBackColor = true;
            this.btnConnectDisconnet.Click += new System.EventHandler(this.btnConnectDisconnect_Click);
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Location = new System.Drawing.Point(13, 141);
            this.lblMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(96, 20);
            this.lblMessage.TabIndex = 2;
            this.lblMessage.Text = "Сообщение";
            // 
            // tbPath
            // 
            this.tbPath.Location = new System.Drawing.Point(141, 78);
            this.tbPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbPath.Name = "tbPath";
            this.tbPath.Size = new System.Drawing.Size(270, 26);
            this.tbPath.TabIndex = 0;
            this.tbPath.Text = ".\\private$\\ServerQueue";
            // 
            // tbUserName
            // 
            this.tbUserName.Location = new System.Drawing.Point(174, 23);
            this.tbUserName.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbUserName.Name = "tbUserName";
            this.tbUserName.Size = new System.Drawing.Size(386, 26);
            this.tbUserName.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 28);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(153, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "Имя пользователя";
            // 
            // rtbMessages
            // 
            this.rtbMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbMessages.Location = new System.Drawing.Point(0, 206);
            this.rtbMessages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rtbMessages.Name = "rtbMessages";
            this.rtbMessages.ReadOnly = true;
            this.rtbMessages.Size = new System.Drawing.Size(573, 395);
            this.rtbMessages.TabIndex = 6;
            this.rtbMessages.Text = "";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(573, 602);
            this.Controls.Add(this.rtbMessages);
            this.Controls.Add(this.tbUserName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbPath);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblIP);
            this.Controls.Add(this.tbMessage);
            this.Controls.Add(this.btnConnectDisconnet);
            this.Controls.Add(this.btnSend);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Клиент";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.Label lblIP;
        private System.Windows.Forms.Button btnConnectDisconnet;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.TextBox tbPath;
        private System.Windows.Forms.TextBox tbUserName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtbMessages;
    }
}

