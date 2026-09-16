namespace AutoSystem
{
    partial class MainFrom
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrom));
            this.lblLog = new System.Windows.Forms.Label();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.btnConfig = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblStation4Status = new System.Windows.Forms.Label();
            this.lblStation3Status = new System.Windows.Forms.Label();
            this.lblStation2Status = new System.Windows.Forms.Label();
            this.lblStation1Status = new System.Windows.Forms.Label();
            this.grpStation4 = new System.Windows.Forms.GroupBox();
            this.lblbarcode_4B = new System.Windows.Forms.Label();
            this.lblbarcode_4A = new System.Windows.Forms.Label();
            this.lblReadbarcode_4B = new System.Windows.Forms.Label();
            this.lblPackageLayer_4B = new System.Windows.Forms.Label();
            this.lblPrint_4B = new System.Windows.Forms.Label();
            this.lblReadbarcode_4A = new System.Windows.Forms.Label();
            this.lblPackageLayer_4A = new System.Windows.Forms.Label();
            this.lblPrint_4A = new System.Windows.Forms.Label();
            this.lblSignalStatus_4B = new System.Windows.Forms.Label();
            this.lblSignalStatus_4A = new System.Windows.Forms.Label();
            this.grpStation3 = new System.Windows.Forms.GroupBox();
            this.lblbarcode_3A = new System.Windows.Forms.Label();
            this.lblSignalStatus_3A = new System.Windows.Forms.Label();
            this.lblbarcode_3D = new System.Windows.Forms.Label();
            this.lblbarcode_3C = new System.Windows.Forms.Label();
            this.lblbarcode_3B = new System.Windows.Forms.Label();
            this.lblSignalStatus_3D = new System.Windows.Forms.Label();
            this.lblSignalStatus_3B = new System.Windows.Forms.Label();
            this.lblSignalStatus_3C = new System.Windows.Forms.Label();
            this.grpStation2 = new System.Windows.Forms.GroupBox();
            this.lblbarcode_2 = new System.Windows.Forms.Label();
            this.lblSignalStatus_2 = new System.Windows.Forms.Label();
            this.grpStation1 = new System.Windows.Forms.GroupBox();
            this.lblbarcode_1 = new System.Windows.Forms.Label();
            this.lblReadbarcode_1 = new System.Windows.Forms.Label();
            this.lblSignalStatus_1 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.lblProductCode = new System.Windows.Forms.Label();
            this.Cbxvendor = new System.Windows.Forms.ComboBox();
            this.DtPdateStr = new System.Windows.Forms.DateTimePicker();
            this.grpStation4.SuspendLayout();
            this.grpStation3.SuspendLayout();
            this.grpStation2.SuspendLayout();
            this.grpStation1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblLog
            // 
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(6, 399);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(59, 12);
            this.lblLog.TabIndex = 44;
            this.lblLog.Text = "系统日志:";
            // 
            // lstLog
            // 
            this.lstLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lstLog.FormattingEnabled = true;
            this.lstLog.ItemHeight = 12;
            this.lstLog.Location = new System.Drawing.Point(8, 424);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new System.Drawing.Size(812, 412);
            this.lstLog.TabIndex = 42;
            // 
            // btnConfig
            // 
            this.btnConfig.Location = new System.Drawing.Point(323, 69);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Size = new System.Drawing.Size(75, 23);
            this.btnConfig.TabIndex = 37;
            this.btnConfig.Text = "配置";
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(226, 69);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 36;
            this.btnReset.Text = "重置";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.LightCoral;
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(139, 69);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 35;
            this.btnStop.Text = "停止系统";
            this.btnStop.UseVisualStyleBackColor = false;
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.LightGreen;
            this.btnStart.Location = new System.Drawing.Point(58, 69);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 34;
            this.btnStart.Text = "启动系统";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTitle.Location = new System.Drawing.Point(14, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(145, 30);
            this.lblTitle.TabIndex = 31;
            this.lblTitle.Text = "产品检测系统";
            // 
            // lblStation4Status
            // 
            this.lblStation4Status.AutoSize = true;
            this.lblStation4Status.Location = new System.Drawing.Point(33, 339);
            this.lblStation4Status.Name = "lblStation4Status";
            this.lblStation4Status.Size = new System.Drawing.Size(107, 12);
            this.lblStation4Status.TabIndex = 48;
            this.lblStation4Status.Text = "lblStation4Status";
            this.lblStation4Status.Visible = false;
            // 
            // lblStation3Status
            // 
            this.lblStation3Status.AutoSize = true;
            this.lblStation3Status.Location = new System.Drawing.Point(33, 314);
            this.lblStation3Status.Name = "lblStation3Status";
            this.lblStation3Status.Size = new System.Drawing.Size(107, 12);
            this.lblStation3Status.TabIndex = 47;
            this.lblStation3Status.Text = "lblStation3Status";
            this.lblStation3Status.Visible = false;
            // 
            // lblStation2Status
            // 
            this.lblStation2Status.AutoSize = true;
            this.lblStation2Status.Location = new System.Drawing.Point(33, 288);
            this.lblStation2Status.Name = "lblStation2Status";
            this.lblStation2Status.Size = new System.Drawing.Size(107, 12);
            this.lblStation2Status.TabIndex = 46;
            this.lblStation2Status.Text = "lblStation2Status";
            this.lblStation2Status.Visible = false;
            // 
            // lblStation1Status
            // 
            this.lblStation1Status.AutoSize = true;
            this.lblStation1Status.Location = new System.Drawing.Point(33, 262);
            this.lblStation1Status.Name = "lblStation1Status";
            this.lblStation1Status.Size = new System.Drawing.Size(107, 12);
            this.lblStation1Status.TabIndex = 45;
            this.lblStation1Status.Text = "lblStation1Status";
            this.lblStation1Status.Visible = false;
            // 
            // grpStation4
            // 
            this.grpStation4.Controls.Add(this.lblbarcode_4B);
            this.grpStation4.Controls.Add(this.lblbarcode_4A);
            this.grpStation4.Controls.Add(this.lblReadbarcode_4B);
            this.grpStation4.Controls.Add(this.lblPackageLayer_4B);
            this.grpStation4.Controls.Add(this.lblPrint_4B);
            this.grpStation4.Controls.Add(this.lblReadbarcode_4A);
            this.grpStation4.Controls.Add(this.lblPackageLayer_4A);
            this.grpStation4.Controls.Add(this.lblPrint_4A);
            this.grpStation4.Controls.Add(this.lblSignalStatus_4B);
            this.grpStation4.Controls.Add(this.lblSignalStatus_4A);
            this.grpStation4.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.grpStation4.Location = new System.Drawing.Point(552, 115);
            this.grpStation4.Name = "grpStation4";
            this.grpStation4.Size = new System.Drawing.Size(270, 167);
            this.grpStation4.TabIndex = 50;
            this.grpStation4.TabStop = false;
            this.grpStation4.Text = "第四站: 打印站";
            // 
            // lblbarcode_4B
            // 
            this.lblbarcode_4B.AutoSize = true;
            this.lblbarcode_4B.Location = new System.Drawing.Point(135, 139);
            this.lblbarcode_4B.Name = "lblbarcode_4B";
            this.lblbarcode_4B.Size = new System.Drawing.Size(59, 17);
            this.lblbarcode_4B.TabIndex = 10;
            this.lblbarcode_4B.Text = "D50410: ";
            // 
            // lblbarcode_4A
            // 
            this.lblbarcode_4A.AutoSize = true;
            this.lblbarcode_4A.Location = new System.Drawing.Point(6, 139);
            this.lblbarcode_4A.Name = "lblbarcode_4A";
            this.lblbarcode_4A.Size = new System.Drawing.Size(59, 17);
            this.lblbarcode_4A.TabIndex = 9;
            this.lblbarcode_4A.Text = "D50310: ";
            // 
            // lblReadbarcode_4B
            // 
            this.lblReadbarcode_4B.AutoSize = true;
            this.lblReadbarcode_4B.Location = new System.Drawing.Point(135, 112);
            this.lblReadbarcode_4B.Name = "lblReadbarcode_4B";
            this.lblReadbarcode_4B.Size = new System.Drawing.Size(102, 17);
            this.lblReadbarcode_4B.TabIndex = 8;
            this.lblReadbarcode_4B.Text = "读码D50404: 0/1";
            // 
            // lblPackageLayer_4B
            // 
            this.lblPackageLayer_4B.AutoSize = true;
            this.lblPackageLayer_4B.Location = new System.Drawing.Point(135, 83);
            this.lblPackageLayer_4B.Name = "lblPackageLayer_4B";
            this.lblPackageLayer_4B.Size = new System.Drawing.Size(102, 17);
            this.lblPackageLayer_4B.TabIndex = 7;
            this.lblPackageLayer_4B.Text = "层数D50403: 1/2";
            // 
            // lblPrint_4B
            // 
            this.lblPrint_4B.AutoSize = true;
            this.lblPrint_4B.Location = new System.Drawing.Point(135, 52);
            this.lblPrint_4B.Name = "lblPrint_4B";
            this.lblPrint_4B.Size = new System.Drawing.Size(102, 17);
            this.lblPrint_4B.TabIndex = 6;
            this.lblPrint_4B.Text = "贴标D50402: 0/1";
            // 
            // lblReadbarcode_4A
            // 
            this.lblReadbarcode_4A.AutoSize = true;
            this.lblReadbarcode_4A.Location = new System.Drawing.Point(6, 112);
            this.lblReadbarcode_4A.Name = "lblReadbarcode_4A";
            this.lblReadbarcode_4A.Size = new System.Drawing.Size(102, 17);
            this.lblReadbarcode_4A.TabIndex = 5;
            this.lblReadbarcode_4A.Text = "读码D50304: 0/1";
            // 
            // lblPackageLayer_4A
            // 
            this.lblPackageLayer_4A.AutoSize = true;
            this.lblPackageLayer_4A.Location = new System.Drawing.Point(6, 83);
            this.lblPackageLayer_4A.Name = "lblPackageLayer_4A";
            this.lblPackageLayer_4A.Size = new System.Drawing.Size(102, 17);
            this.lblPackageLayer_4A.TabIndex = 4;
            this.lblPackageLayer_4A.Text = "层数D50303: 1/2";
            // 
            // lblPrint_4A
            // 
            this.lblPrint_4A.AutoSize = true;
            this.lblPrint_4A.Location = new System.Drawing.Point(6, 52);
            this.lblPrint_4A.Name = "lblPrint_4A";
            this.lblPrint_4A.Size = new System.Drawing.Size(102, 17);
            this.lblPrint_4A.TabIndex = 3;
            this.lblPrint_4A.Text = "贴标D50302: 0/1";
            // 
            // lblSignalStatus_4B
            // 
            this.lblSignalStatus_4B.AutoSize = true;
            this.lblSignalStatus_4B.Location = new System.Drawing.Point(135, 26);
            this.lblSignalStatus_4B.Name = "lblSignalStatus_4B";
            this.lblSignalStatus_4B.Size = new System.Drawing.Size(78, 17);
            this.lblSignalStatus_4B.TabIndex = 2;
            this.lblSignalStatus_4B.Text = "D50401: 0/1";
            // 
            // lblSignalStatus_4A
            // 
            this.lblSignalStatus_4A.AutoSize = true;
            this.lblSignalStatus_4A.Location = new System.Drawing.Point(6, 23);
            this.lblSignalStatus_4A.Name = "lblSignalStatus_4A";
            this.lblSignalStatus_4A.Size = new System.Drawing.Size(78, 17);
            this.lblSignalStatus_4A.TabIndex = 1;
            this.lblSignalStatus_4A.Text = "D50301: 0/1";
            // 
            // grpStation3
            // 
            this.grpStation3.Controls.Add(this.lblbarcode_3A);
            this.grpStation3.Controls.Add(this.lblSignalStatus_3A);
            this.grpStation3.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.grpStation3.Location = new System.Drawing.Point(334, 118);
            this.grpStation3.Name = "grpStation3";
            this.grpStation3.Size = new System.Drawing.Size(213, 100);
            this.grpStation3.TabIndex = 51;
            this.grpStation3.TabStop = false;
            this.grpStation3.Text = "第三站: 四面检测站";
            // 
            // lblbarcode_3A
            // 
            this.lblbarcode_3A.AutoSize = true;
            this.lblbarcode_3A.Location = new System.Drawing.Point(16, 43);
            this.lblbarcode_3A.Name = "lblbarcode_3A";
            this.lblbarcode_3A.Size = new System.Drawing.Size(59, 17);
            this.lblbarcode_3A.TabIndex = 12;
            this.lblbarcode_3A.Text = "D50210: ";
            // 
            // lblSignalStatus_3A
            // 
            this.lblSignalStatus_3A.AutoSize = true;
            this.lblSignalStatus_3A.Location = new System.Drawing.Point(16, 26);
            this.lblSignalStatus_3A.Name = "lblSignalStatus_3A";
            this.lblSignalStatus_3A.Size = new System.Drawing.Size(102, 17);
            this.lblSignalStatus_3A.TabIndex = 1;
            this.lblSignalStatus_3A.Text = "D50201: 1/2/3/4";
            // 
            // lblbarcode_3D
            // 
            this.lblbarcode_3D.AutoSize = true;
            this.lblbarcode_3D.Location = new System.Drawing.Point(701, 378);
            this.lblbarcode_3D.Name = "lblbarcode_3D";
            this.lblbarcode_3D.Size = new System.Drawing.Size(41, 12);
            this.lblbarcode_3D.TabIndex = 15;
            this.lblbarcode_3D.Text = "D304: ";
            this.lblbarcode_3D.Visible = false;
            // 
            // lblbarcode_3C
            // 
            this.lblbarcode_3C.AutoSize = true;
            this.lblbarcode_3C.Location = new System.Drawing.Point(701, 365);
            this.lblbarcode_3C.Name = "lblbarcode_3C";
            this.lblbarcode_3C.Size = new System.Drawing.Size(41, 12);
            this.lblbarcode_3C.TabIndex = 14;
            this.lblbarcode_3C.Text = "D303: ";
            this.lblbarcode_3C.Visible = false;
            // 
            // lblbarcode_3B
            // 
            this.lblbarcode_3B.AutoSize = true;
            this.lblbarcode_3B.Location = new System.Drawing.Point(701, 353);
            this.lblbarcode_3B.Name = "lblbarcode_3B";
            this.lblbarcode_3B.Size = new System.Drawing.Size(41, 12);
            this.lblbarcode_3B.TabIndex = 13;
            this.lblbarcode_3B.Text = "D302: ";
            this.lblbarcode_3B.Visible = false;
            // 
            // lblSignalStatus_3D
            // 
            this.lblSignalStatus_3D.AutoSize = true;
            this.lblSignalStatus_3D.Location = new System.Drawing.Point(619, 380);
            this.lblSignalStatus_3D.Name = "lblSignalStatus_3D";
            this.lblSignalStatus_3D.Size = new System.Drawing.Size(65, 12);
            this.lblSignalStatus_3D.TabIndex = 4;
            this.lblSignalStatus_3D.Text = "D1034: 0/1";
            this.lblSignalStatus_3D.Visible = false;
            // 
            // lblSignalStatus_3B
            // 
            this.lblSignalStatus_3B.AutoSize = true;
            this.lblSignalStatus_3B.Location = new System.Drawing.Point(620, 353);
            this.lblSignalStatus_3B.Name = "lblSignalStatus_3B";
            this.lblSignalStatus_3B.Size = new System.Drawing.Size(65, 12);
            this.lblSignalStatus_3B.TabIndex = 3;
            this.lblSignalStatus_3B.Text = "D1032: 0/1";
            this.lblSignalStatus_3B.Visible = false;
            // 
            // lblSignalStatus_3C
            // 
            this.lblSignalStatus_3C.AutoSize = true;
            this.lblSignalStatus_3C.Location = new System.Drawing.Point(620, 365);
            this.lblSignalStatus_3C.Name = "lblSignalStatus_3C";
            this.lblSignalStatus_3C.Size = new System.Drawing.Size(65, 12);
            this.lblSignalStatus_3C.TabIndex = 2;
            this.lblSignalStatus_3C.Text = "D1033: 0/1";
            this.lblSignalStatus_3C.Visible = false;
            // 
            // grpStation2
            // 
            this.grpStation2.Controls.Add(this.lblbarcode_2);
            this.grpStation2.Controls.Add(this.lblSignalStatus_2);
            this.grpStation2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.grpStation2.Location = new System.Drawing.Point(172, 118);
            this.grpStation2.Name = "grpStation2";
            this.grpStation2.Size = new System.Drawing.Size(156, 100);
            this.grpStation2.TabIndex = 52;
            this.grpStation2.TabStop = false;
            this.grpStation2.Text = "第二站: 上盖检测站";
            // 
            // lblbarcode_2
            // 
            this.lblbarcode_2.AutoSize = true;
            this.lblbarcode_2.Location = new System.Drawing.Point(12, 43);
            this.lblbarcode_2.Name = "lblbarcode_2";
            this.lblbarcode_2.Size = new System.Drawing.Size(59, 17);
            this.lblbarcode_2.TabIndex = 12;
            this.lblbarcode_2.Text = "D50110: ";
            // 
            // lblSignalStatus_2
            // 
            this.lblSignalStatus_2.AutoSize = true;
            this.lblSignalStatus_2.Location = new System.Drawing.Point(12, 23);
            this.lblSignalStatus_2.Name = "lblSignalStatus_2";
            this.lblSignalStatus_2.Size = new System.Drawing.Size(78, 17);
            this.lblSignalStatus_2.TabIndex = 1;
            this.lblSignalStatus_2.Text = "D50101: 0/1";
            // 
            // grpStation1
            // 
            this.grpStation1.Controls.Add(this.lblbarcode_1);
            this.grpStation1.Controls.Add(this.lblReadbarcode_1);
            this.grpStation1.Controls.Add(this.lblSignalStatus_1);
            this.grpStation1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.grpStation1.Location = new System.Drawing.Point(16, 118);
            this.grpStation1.Name = "grpStation1";
            this.grpStation1.Size = new System.Drawing.Size(150, 100);
            this.grpStation1.TabIndex = 49;
            this.grpStation1.TabStop = false;
            this.grpStation1.Text = "第一站: 卡扣监测站";
            // 
            // lblbarcode_1
            // 
            this.lblbarcode_1.AutoSize = true;
            this.lblbarcode_1.Location = new System.Drawing.Point(22, 69);
            this.lblbarcode_1.Name = "lblbarcode_1";
            this.lblbarcode_1.Size = new System.Drawing.Size(59, 17);
            this.lblbarcode_1.TabIndex = 11;
            this.lblbarcode_1.Text = "D50010: ";
            // 
            // lblReadbarcode_1
            // 
            this.lblReadbarcode_1.AutoSize = true;
            this.lblReadbarcode_1.Location = new System.Drawing.Point(22, 52);
            this.lblReadbarcode_1.Name = "lblReadbarcode_1";
            this.lblReadbarcode_1.Size = new System.Drawing.Size(78, 17);
            this.lblReadbarcode_1.TabIndex = 10;
            this.lblReadbarcode_1.Text = "D50001: 1/2";
            // 
            // lblSignalStatus_1
            // 
            this.lblSignalStatus_1.AutoSize = true;
            this.lblSignalStatus_1.Location = new System.Drawing.Point(22, 23);
            this.lblSignalStatus_1.Name = "lblSignalStatus_1";
            this.lblSignalStatus_1.Size = new System.Drawing.Size(78, 17);
            this.lblSignalStatus_1.TabIndex = 0;
            this.lblSignalStatus_1.Text = "D50000: 0/1";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(434, 47);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(59, 12);
            this.label19.TabIndex = 160;
            this.label19.Text = "生产日期:";
            // 
            // lblProductCode
            // 
            this.lblProductCode.AutoSize = true;
            this.lblProductCode.Location = new System.Drawing.Point(434, 20);
            this.lblProductCode.Name = "lblProductCode";
            this.lblProductCode.Size = new System.Drawing.Size(47, 12);
            this.lblProductCode.TabIndex = 159;
            this.lblProductCode.Text = "厂商号:";
            // 
            // Cbxvendor
            // 
            this.Cbxvendor.FormattingEnabled = true;
            this.Cbxvendor.Items.AddRange(new object[] {
            "vendor01",
            "A01",
            "A02",
            "Q01"});
            this.Cbxvendor.Location = new System.Drawing.Point(511, 17);
            this.Cbxvendor.Name = "Cbxvendor";
            this.Cbxvendor.Size = new System.Drawing.Size(121, 20);
            this.Cbxvendor.TabIndex = 161;
            // 
            // DtPdateStr
            // 
            this.DtPdateStr.Location = new System.Drawing.Point(511, 47);
            this.DtPdateStr.Name = "DtPdateStr";
            this.DtPdateStr.Size = new System.Drawing.Size(121, 21);
            this.DtPdateStr.TabIndex = 162;
            // 
            // MainFrom
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 848);
            this.Controls.Add(this.lblbarcode_3D);
            this.Controls.Add(this.lblSignalStatus_3D);
            this.Controls.Add(this.DtPdateStr);
            this.Controls.Add(this.lblSignalStatus_3C);
            this.Controls.Add(this.lblSignalStatus_3B);
            this.Controls.Add(this.lblbarcode_3C);
            this.Controls.Add(this.Cbxvendor);
            this.Controls.Add(this.lblbarcode_3B);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.lblProductCode);
            this.Controls.Add(this.lblStation4Status);
            this.Controls.Add(this.lblStation3Status);
            this.Controls.Add(this.lblStation2Status);
            this.Controls.Add(this.lblStation1Status);
            this.Controls.Add(this.grpStation4);
            this.Controls.Add(this.grpStation3);
            this.Controls.Add(this.grpStation2);
            this.Controls.Add(this.grpStation1);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.btnConfig);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.lblTitle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainFrom";
            this.Text = "自动检测系统";
            this.grpStation4.ResumeLayout(false);
            this.grpStation4.PerformLayout();
            this.grpStation3.ResumeLayout(false);
            this.grpStation3.PerformLayout();
            this.grpStation2.ResumeLayout(false);
            this.grpStation2.PerformLayout();
            this.grpStation1.ResumeLayout(false);
            this.grpStation1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.ListBox lstLog;
        private System.Windows.Forms.Button btnConfig;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStation4Status;
        private System.Windows.Forms.Label lblStation3Status;
        private System.Windows.Forms.Label lblStation2Status;
        private System.Windows.Forms.Label lblStation1Status;
        private System.Windows.Forms.GroupBox grpStation4;
        private System.Windows.Forms.Label lblbarcode_4B;
        private System.Windows.Forms.Label lblbarcode_4A;
        private System.Windows.Forms.Label lblReadbarcode_4B;
        private System.Windows.Forms.Label lblPackageLayer_4B;
        private System.Windows.Forms.Label lblPrint_4B;
        private System.Windows.Forms.Label lblReadbarcode_4A;
        private System.Windows.Forms.Label lblPackageLayer_4A;
        private System.Windows.Forms.Label lblPrint_4A;
        private System.Windows.Forms.Label lblSignalStatus_4B;
        private System.Windows.Forms.Label lblSignalStatus_4A;
        private System.Windows.Forms.GroupBox grpStation3;
        private System.Windows.Forms.Label lblbarcode_3D;
        private System.Windows.Forms.Label lblbarcode_3C;
        private System.Windows.Forms.Label lblbarcode_3B;
        private System.Windows.Forms.Label lblbarcode_3A;
        private System.Windows.Forms.Label lblSignalStatus_3D;
        private System.Windows.Forms.Label lblSignalStatus_3B;
        private System.Windows.Forms.Label lblSignalStatus_3C;
        private System.Windows.Forms.Label lblSignalStatus_3A;
        private System.Windows.Forms.GroupBox grpStation2;
        private System.Windows.Forms.Label lblbarcode_2;
        private System.Windows.Forms.Label lblSignalStatus_2;
        private System.Windows.Forms.GroupBox grpStation1;
        private System.Windows.Forms.Label lblbarcode_1;
        private System.Windows.Forms.Label lblReadbarcode_1;
        private System.Windows.Forms.Label lblSignalStatus_1;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label lblProductCode;
        private System.Windows.Forms.ComboBox Cbxvendor;
        private System.Windows.Forms.DateTimePicker DtPdateStr;
    }
}

