namespace PlanServerTaskManager
{
    partial class ParaSet
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParaSet));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtRunMin = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTm = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.lstDays = new System.Windows.Forms.ComboBox();
            this.colDel = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colDay = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRunMinute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEndTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label4 = new System.Windows.Forms.Label();
            this.txtResult = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDel,
            this.colDay,
            this.colTime,
            this.colRunMinute,
            this.colEndTime});
            this.dataGridView1.Location = new System.Drawing.Point(1, 36);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(660, 182);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.Location = new System.Drawing.Point(577, 251);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 34);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "完成";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.Location = new System.Drawing.Point(577, 302);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtRunMin
            // 
            this.txtRunMin.Location = new System.Drawing.Point(302, 9);
            this.txtRunMin.Name = "txtRunMin";
            this.txtRunMin.Size = new System.Drawing.Size(56, 21);
            this.txtRunMin.TabIndex = 2;
            this.txtRunMin.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(237, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "运行时长:";
            // 
            // txtTm
            // 
            this.txtTm.Location = new System.Drawing.Point(164, 9);
            this.txtTm.Name = "txtTm";
            this.txtTm.Size = new System.Drawing.Size(56, 21);
            this.txtTm.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(99, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "几点几分:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(364, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "分钟(0为永久)";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(485, 7);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(97, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "添加";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // lstDays
            // 
            this.lstDays.DropDownHeight = 1060;
            this.lstDays.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstDays.FormattingEnabled = true;
            this.lstDays.IntegralHeight = false;
            this.lstDays.ItemHeight = 12;
            this.lstDays.Location = new System.Drawing.Point(9, 9);
            this.lstDays.Name = "lstDays";
            this.lstDays.Size = new System.Drawing.Size(76, 20);
            this.lstDays.TabIndex = 4;
            // 
            // colDel
            // 
            this.colDel.Frozen = true;
            this.colDel.HeaderText = "";
            this.colDel.Name = "colDel";
            this.colDel.ReadOnly = true;
            this.colDel.Width = 60;
            // 
            // colDay
            // 
            this.colDay.Frozen = true;
            this.colDay.HeaderText = "运行日期";
            this.colDay.Name = "colDay";
            this.colDay.ReadOnly = true;
            this.colDay.Width = 85;
            // 
            // colTime
            // 
            this.colTime.Frozen = true;
            this.colTime.HeaderText = "启动时间";
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.Width = 60;
            // 
            // colRunMinute
            // 
            this.colRunMinute.Frozen = true;
            this.colRunMinute.HeaderText = "运行几分钟";
            this.colRunMinute.Name = "colRunMinute";
            this.colRunMinute.ReadOnly = true;
            this.colRunMinute.Width = 110;
            // 
            // colEndTime
            // 
            this.colEndTime.Frozen = true;
            this.colEndTime.HeaderText = "结束时间";
            this.colEndTime.Name = "colEndTime";
            this.colEndTime.ReadOnly = true;
            this.colEndTime.Width = 300;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 252);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(503, 84);
            this.label4.TabIndex = 3;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(1, 224);
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(660, 21);
            this.txtResult.TabIndex = 2;
            // 
            // ParaSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 340);
            this.Controls.Add(this.lstDays);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.txtTm);
            this.Controls.Add(this.txtRunMin);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dataGridView1);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ParaSet";
            this.Text = "ParaSet";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ParaSet_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtRunMin;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTm;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.ComboBox lstDays;
        private System.Windows.Forms.DataGridViewLinkColumn colDel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDay;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunMinute;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEndTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtResult;
    }
}