namespace PlanServerTaskManager
{
    partial class Form1
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.labSocket = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.chkCheckFile = new System.Windows.Forms.CheckBox();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnSaveToDb = new System.Windows.Forms.Button();
            this.btnSelectDbFile = new System.Windows.Forms.Button();
            this.dgvTask = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.txtSql = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnRunSql = new System.Windows.Forms.Button();
            this.dgvSqlData = new System.Windows.Forms.DataGridView();
            this.colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDel = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colSave = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExepath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExepara = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRuntype = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colTaskPara = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRuntime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPidTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInsTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTask)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSqlData)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1154, 543);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.splitContainer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1146, 517);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "任务清单";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.labSocket);
            this.splitContainer1.Panel1.Controls.Add(this.btnSend);
            this.splitContainer1.Panel1.Controls.Add(this.txtPort);
            this.splitContainer1.Panel1.Controls.Add(this.txtServerIP);
            this.splitContainer1.Panel1.Controls.Add(this.chkCheckFile);
            this.splitContainer1.Panel1.Controls.Add(this.btnHelp);
            this.splitContainer1.Panel1.Controls.Add(this.btnSaveToDb);
            this.splitContainer1.Panel1.Controls.Add(this.btnSelectDbFile);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvTask);
            this.splitContainer1.Size = new System.Drawing.Size(1140, 511);
            this.splitContainer1.SplitterDistance = 43;
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(339, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "IP与端口";
            // 
            // labSocket
            // 
            this.labSocket.AutoSize = true;
            this.labSocket.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labSocket.Location = new System.Drawing.Point(628, 2);
            this.labSocket.Name = "labSocket";
            this.labSocket.Size = new System.Drawing.Size(73, 12);
            this.labSocket.TabIndex = 4;
            this.labSocket.Text = "Socket消息";
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(546, 9);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "连接服务器";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(498, 11);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(42, 21);
            this.txtPort.TabIndex = 2;
            this.txtPort.Text = "23244";
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(392, 11);
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(100, 21);
            this.txtServerIP.TabIndex = 2;
            this.txtServerIP.Text = "192.168.54.40";
            // 
            // chkCheckFile
            // 
            this.chkCheckFile.AutoSize = true;
            this.chkCheckFile.Checked = true;
            this.chkCheckFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCheckFile.Location = new System.Drawing.Point(151, 13);
            this.chkCheckFile.Name = "chkCheckFile";
            this.chkCheckFile.Size = new System.Drawing.Size(174, 16);
            this.chkCheckFile.TabIndex = 1;
            this.chkCheckFile.Text = "保存时检查exe文件是否存在";
            this.chkCheckFile.UseVisualStyleBackColor = true;
            this.chkCheckFile.Visible = false;
            // 
            // btnHelp
            // 
            this.btnHelp.Location = new System.Drawing.Point(1060, 10);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(75, 23);
            this.btnHelp.TabIndex = 0;
            this.btnHelp.Text = "帮助";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            // 
            // btnSaveToDb
            // 
            this.btnSaveToDb.Location = new System.Drawing.Point(968, 10);
            this.btnSaveToDb.Name = "btnSaveToDb";
            this.btnSaveToDb.Size = new System.Drawing.Size(75, 23);
            this.btnSaveToDb.TabIndex = 0;
            this.btnSaveToDb.Text = "保存全部";
            this.btnSaveToDb.UseVisualStyleBackColor = true;
            this.btnSaveToDb.Visible = false;
            this.btnSaveToDb.Click += new System.EventHandler(this.btnSaveToDb_Click);
            // 
            // btnSelectDbFile
            // 
            this.btnSelectDbFile.Location = new System.Drawing.Point(5, 9);
            this.btnSelectDbFile.Name = "btnSelectDbFile";
            this.btnSelectDbFile.Size = new System.Drawing.Size(140, 23);
            this.btnSelectDbFile.TabIndex = 0;
            this.btnSelectDbFile.Text = "选择本地SQLite数据库";
            this.btnSelectDbFile.UseVisualStyleBackColor = true;
            this.btnSelectDbFile.Click += new System.EventHandler(this.btnSelectDbFile_Click);
            // 
            // dgvTask
            // 
            this.dgvTask.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTask.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colId,
            this.colDel,
            this.colSave,
            this.colDesc,
            this.colExepath,
            this.colExepara,
            this.colRuntype,
            this.colTaskPara,
            this.colRuntime,
            this.colPid,
            this.colPidTime,
            this.colInsTime,
            this.colStatus});
            this.dgvTask.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTask.Location = new System.Drawing.Point(0, 0);
            this.dgvTask.Name = "dgvTask";
            this.dgvTask.RowTemplate.Height = 23;
            this.dgvTask.Size = new System.Drawing.Size(1140, 467);
            this.dgvTask.TabIndex = 0;
            this.dgvTask.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTask_CellClick);
            this.dgvTask.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTask_CellMouseEnter);
            this.dgvTask.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvTask_CurrentCellDirtyStateChanged);
            this.dgvTask.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvTask_RowsAdded);
            this.dgvTask.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgvTask_RowsRemoved);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1146, 517);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Sql";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.dgvSqlData);
            this.splitContainer2.Size = new System.Drawing.Size(1140, 512);
            this.splitContainer2.SplitterDistance = 168;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer3.IsSplitterFixed = true;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.txtSql);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.label1);
            this.splitContainer3.Panel2.Controls.Add(this.btnRunSql);
            this.splitContainer3.Size = new System.Drawing.Size(1140, 168);
            this.splitContainer3.SplitterDistance = 142;
            this.splitContainer3.SplitterWidth = 1;
            this.splitContainer3.TabIndex = 0;
            // 
            // txtSql
            // 
            this.txtSql.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSql.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSql.Location = new System.Drawing.Point(0, 0);
            this.txtSql.Multiline = true;
            this.txtSql.Name = "txtSql";
            this.txtSql.Size = new System.Drawing.Size(1140, 142);
            this.txtSql.TabIndex = 0;
            this.txtSql.Text = "select * from tasks";
            this.txtSql.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtSql_MouseDoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(87, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 12);
            this.label1.TabIndex = 1;
            // 
            // btnRunSql
            // 
            this.btnRunSql.Location = new System.Drawing.Point(5, 2);
            this.btnRunSql.Name = "btnRunSql";
            this.btnRunSql.Size = new System.Drawing.Size(75, 23);
            this.btnRunSql.TabIndex = 0;
            this.btnRunSql.Text = "执行sql";
            this.btnRunSql.UseVisualStyleBackColor = true;
            this.btnRunSql.Click += new System.EventHandler(this.btnRunSql_Click);
            // 
            // dgvSqlData
            // 
            this.dgvSqlData.AllowUserToAddRows = false;
            this.dgvSqlData.AllowUserToDeleteRows = false;
            this.dgvSqlData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSqlData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSqlData.Location = new System.Drawing.Point(0, 0);
            this.dgvSqlData.Name = "dgvSqlData";
            this.dgvSqlData.ReadOnly = true;
            this.dgvSqlData.RowTemplate.Height = 23;
            this.dgvSqlData.Size = new System.Drawing.Size(1140, 343);
            this.dgvSqlData.TabIndex = 0;
            // 
            // colId
            // 
            this.colId.FillWeight = 151F;
            this.colId.Frozen = true;
            this.colId.HeaderText = "id";
            this.colId.Name = "colId";
            this.colId.ReadOnly = true;
            this.colId.Width = 20;
            // 
            // colDel
            // 
            this.colDel.HeaderText = "";
            this.colDel.Name = "colDel";
            this.colDel.ReadOnly = true;
            this.colDel.Text = "";
            this.colDel.Width = 35;
            // 
            // colSave
            // 
            this.colSave.HeaderText = "";
            this.colSave.Name = "colSave";
            this.colSave.ReadOnly = true;
            this.colSave.Width = 35;
            // 
            // colDesc
            // 
            this.colDesc.HeaderText = "任务说明";
            this.colDesc.Name = "colDesc";
            this.colDesc.Width = 130;
            // 
            // colExepath
            // 
            this.colExepath.HeaderText = "exe路径（服务器上的路径）";
            this.colExepath.Name = "colExepath";
            this.colExepath.Width = 250;
            // 
            // colExepara
            // 
            this.colExepara.HeaderText = "参数";
            this.colExepara.Name = "colExepara";
            this.colExepara.Width = 50;
            // 
            // colRuntype
            // 
            this.colRuntype.HeaderText = "运行类型";
            this.colRuntype.MaxDropDownItems = 100;
            this.colRuntype.Name = "colRuntype";
            this.colRuntype.Width = 210;
            // 
            // colTaskPara
            // 
            this.colTaskPara.HeaderText = "定时参数";
            this.colTaskPara.Name = "colTaskPara";
            this.colTaskPara.ReadOnly = true;
            this.colTaskPara.Width = 80;
            // 
            // colRuntime
            // 
            this.colRuntime.FillWeight = 40F;
            this.colRuntime.HeaderText = "运行次数";
            this.colRuntime.Name = "colRuntime";
            this.colRuntime.ReadOnly = true;
            this.colRuntime.Width = 40;
            // 
            // colPid
            // 
            this.colPid.FillWeight = 30F;
            this.colPid.HeaderText = "pid";
            this.colPid.Name = "colPid";
            this.colPid.ReadOnly = true;
            this.colPid.Width = 30;
            // 
            // colPidTime
            // 
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.colPidTime.DefaultCellStyle = dataGridViewCellStyle1;
            this.colPidTime.HeaderText = "pid时间";
            this.colPidTime.Name = "colPidTime";
            this.colPidTime.ReadOnly = true;
            this.colPidTime.Width = 65;
            // 
            // colInsTime
            // 
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.colInsTime.DefaultCellStyle = dataGridViewCellStyle2;
            this.colInsTime.HeaderText = "创建时间";
            this.colInsTime.Name = "colInsTime";
            this.colInsTime.ReadOnly = true;
            this.colInsTime.Width = 65;
            // 
            // colStatus
            // 
            this.colStatus.HeaderText = "状态";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 55;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1154, 543);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTask)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel1.PerformLayout();
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.Panel2.PerformLayout();
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSqlData)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnSaveToDb;
        private System.Windows.Forms.Button btnSelectDbFile;
        private System.Windows.Forms.DataGridView dgvTask;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.TextBox txtSql;
        private System.Windows.Forms.Button btnRunSql;
        private System.Windows.Forms.DataGridView dgvSqlData;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkCheckFile;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtServerIP;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label labSocket;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewTextBoxColumn colId;
        private System.Windows.Forms.DataGridViewLinkColumn colDel;
        private System.Windows.Forms.DataGridViewLinkColumn colSave;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDesc;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExepath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExepara;
        private System.Windows.Forms.DataGridViewComboBoxColumn colRuntype;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTaskPara;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRuntime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPidTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInsTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
    }
}

