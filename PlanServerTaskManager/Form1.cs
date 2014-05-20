using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PlanServerService;

namespace PlanServerTaskManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // 设置DataGridView为单击进入编辑状态，而不是普通的双击
            dgvTask.EditMode = DataGridViewEditMode.EditOnEnter;
            // 设置protected属性，此属性指示此控件是否应使用辅助缓冲区重绘其图面，以 减少或避免闪烁（CellMouseEnter里修改背景色会导致闪烁）
            typeof(DataGridView).GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dgvTask, true, null);

            dgvTask.RowTemplate.Height = 25;
            //dgvTask.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        string formTitle = "任务计划管理-配合PlanServer使用";
        private void Form1_Load(object sender, EventArgs e)
        {
            Text = formTitle;

            txtServerIP.Text = Common.GetSetting("serverIp", "127.0.0.1");
            txtPort.Text = Common.GetInt32("serverPort", TaskService.ListenPort).ToString();

            if (!Common.GetBoolean("enableLocalDbSelect"))
            {
                btnSelectDbFile.Visible = false;
                chkCheckFile.Visible = false;
            }
            labSocket.Text = string.Empty;
        }

        private Dal dbaccess;

        // 选择数据库文件，并读取出所有任务显示到DataGridView
        private void btnSelectDbFile_Click(object sender, EventArgs e)
        {
            var fdl = new OpenFileDialog { Multiselect = false };
            //fdl.FileName = "";
            DialogResult result = fdl.ShowDialog(this);
            if (result != DialogResult.OK)
            {
                return;
            }
            dbaccess = new Dal(fdl.FileName);
            Text = formTitle + "  " + fdl.FileName;
            if (GetTaskToView(dgvTask))
            {
                btnSaveToDb.Visible = true;
                chkCheckFile.Visible = true;
                isSocketAccess = false;
            }
            else
            {
                //dbaccess = null;
                btnSaveToDb.Visible = false;
                chkCheckFile.Visible = false;
            }
        }

        // 把DataGridView的显示，更新到数据库里去
        private void btnSaveToDb_Click(object sender, EventArgs e)
        {
            List<TaskItem> tasks = GetGridData();
            if (tasks == null)
                return;

            foreach (TaskItem task in tasks)
            {
                if (task.id <= 0)
                    dbaccess.AddTask(task);
                else
                    dbaccess.UpdateTask(task);
            }
            GetTaskToView(dgvTask);
            MessageBox.Show("保存成功");
        }

        // 是访问本地文件数据库 还是通过Socket访问网络服务
        private bool isSocketAccess = false;
        
        #region DataGridView相关事件
        // 一共有几列
        private const int COL_NUM = 13;

        private const int COLIDX_ID = 0;
        private const int COLIDX_DEL = 1;
        private const int COLIDX_SAVE = 2;
        private const int COLIDX_DESC = 3;
        private const int COLIDX_EXEPATH = 4;
        private const int COLIDX_EXEPARA = 5;
        // DataGridView的任务类型列的列号
        private const int COLIDX_TYPE = 6;
        private const int COLIDX_TYPEPARA = 7;
        private const int COLIDX_RUNCOUNT = 8;
        private const int COLIDX_PID = 9;
        private const int COLIDX_PIDTIME = 10;
        private const int COLIDX_INSTIME = 11;
        // DataGridView的删除列的列号
        private const int COLIDX_STATUS = 12;

        #region 鼠标移动时，修改DataGridView的当前行背景色
        private int lastRowIndex = -1;   // 记录最后改变的行号，用于把它改回原来的颜色
        /// <summary>
        /// 鼠标进入时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvTask_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < dgvTask.Rows.Count - 1 && e.RowIndex >= 0 && lastRowIndex != e.RowIndex)
            {
                if (lastRowIndex > -1)
                    SetBackColor(dgvTask.Rows[lastRowIndex], false);
                SetBackColor(dgvTask.Rows[e.RowIndex], true);
                lastRowIndex = e.RowIndex;
            }
        }
        /// <summary>
        /// 设置焦点行的背景色
        /// </summary>
        /// <param name="row"></param>
        /// <param name="isFocus"></param>
        static void SetBackColor(DataGridViewRow row, bool isFocus)
        {
            Color color;
            if (isFocus)
                color = Color.YellowGreen;
            else if (row.Index % 2 == 0)
                color = Color.White;
            else
                color = Color.FromArgb(233, 245, 178);

            row.DefaultCellStyle.BackColor = color;
            //foreach (DataGridViewCell cell in row.Cells)
            //{
            //    cell.Style.BackColor = color;
            //}
        }
        #endregion

        /// <summary>
        /// 当Cell值改变时，立即提交改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvTask_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dgv = (DataGridView) sender;
            if (dgv.CurrentRow == null)
                return;

            // 如果是新行，给最后一格添加删除字样
            if (dgv.CurrentRow.Cells[COLIDX_DEL].Value == null)
            {
                dgv.CurrentRow.Cells[COLIDX_DEL].Value = "删除";
                dgv.CurrentRow.Cells[COLIDX_SAVE].Value = "保存";
                // 设置隔行背景色
                SetBackColor(dgv.CurrentRow, false);
                // 绑定下拉框列表
                BindDataGridViewComboBoxOption(dgv.CurrentRow.Cells[COLIDX_TYPE], RunType.Stop);
            }

            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        void BindDataGridViewComboBoxOption(DataGridViewCell cell, RunType defaultValue)
        {
            var combobox = cell as DataGridViewComboBoxCell;
            if (combobox == null)
                return;
            foreach (var item in GetRunTypeDesc())//Enum.GetNames(typeof(RunType)))
            {
                if (item.Key == (int)RunType.ForceStop)
                    combobox.Items.Insert(0, item.Value);
                else
                    combobox.Items.Add(item.Value);
                if (item.Key.Equals((int)defaultValue))
                    combobox.Value = item.Value;
            }
        }

        private void dgvTask_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            DataGridView dgv = (DataGridView)sender;
            
            #region 点击删除按钮
            if (e.ColumnIndex == COLIDX_DEL)
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count - 1)//Rows.Count - 1是不让点击未提交的新行
                    return;

                var row = dgv.Rows[e.RowIndex];
                string tmp = Convert.ToString(row.Cells[COLIDX_ID].Value);
                int id;
                if(string.IsNullOrEmpty(tmp) || !int.TryParse(tmp, out id))
                {
                    MessageBox.Show("未保存的数据，不能删除");
                    return;
                }

                if (MessageBox.Show("确认要删除吗？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    if (isSocketAccess)
                    {
                        // 通过Socket删除
                        string str = TaskClient.DelTaskById(txtServerIP.Text, int.Parse(txtPort.Text), id);
                        if (!string.IsNullOrEmpty(str))
                        {
                            MessageBox.Show(str);
                            return;
                        }
                        SetSocketStatus("删除成功 ");
                        MessageBox.Show(str);
                    }
                    else
                    {
                        // 本地文件删除
                        dbaccess.DelTaskById(id);
                    }
                    dgv.Rows.RemoveAt(e.RowIndex);
                    dgv.ClearSelection();
                }
                return;
            }
            #endregion

            #region 点击保存按钮
            if (e.ColumnIndex == COLIDX_SAVE)
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count - 1)//Rows.Count - 1是不让点击未提交的新行
                    return;
                
                var row = dgv.Rows[e.RowIndex];
                TaskItem task = GetRowData(row);
                if (task != null)
                {
                    if (isSocketAccess)
                    {
                        // 通过Socket保存
                        string msg;
                        TaskManage tasks = TaskClient.SaveTask(txtServerIP.Text, int.Parse(txtPort.Text), task, out msg);
                        if(tasks == null)
                        {
                            MessageBox.Show(msg);
                            return;
                        }
                        SetSocketStatus("保存成功 \r\n服务器的时间:" + tasks.serverTime + "\r\n上次轮询时间:" + tasks.lastRunTime);
                        if (tasks.tasks != null)
                        {
                            GetTaskToView(dgvTask, tasks.tasks);
                        }
                    }
                    else
                    {
                        // 本地文件保存
                        if (task.id <= 0)
                            dbaccess.AddTask(task);
                        else
                            dbaccess.UpdateTask(task);

                        GetTaskToView(dgvTask);
                    }
                }
                return;
            }

            #endregion

            #region 点击定时参数列
            if (e.ColumnIndex == COLIDX_TYPEPARA)
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count - 1) //Rows.Count - 1是不让点击未提交的新行
                    return;

                var row = dgv.Rows[e.RowIndex];
                //TaskItem task = GetRowData(row);
                //string typepara = Convert.ToString(row.Cells[e.ColumnIndex].Value).Trim();
                var cell = row.Cells[e.ColumnIndex];
                string runtypeStr = Convert.ToString(row.Cells[COLIDX_TYPE].Value).Trim();
                RunType runtype = (from pair in GetRunTypeDesc()
                                   where pair.Value == runtypeStr
                                   select (RunType) pair.Key).FirstOrDefault();
                switch (runtype)
                {
                    case RunType.PerDay:
                    case RunType.PerWeek:
                    case RunType.PerMonth:
                        new ParaSet(runtype, cell).ShowDialog(this);
                        return;
                    default:
                        return;
                }
                //return;
            }

            #endregion
        }

        // 给新行 添加行号
        private void dgvTask_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var dgv = (DataGridView)sender;
            for (int i = 0; i < e.RowCount; i++)
            {
                dgv.Rows[e.RowIndex + i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Rows[e.RowIndex + i].HeaderCell.Value = (e.RowIndex + i + 1).ToString();
            }
            for (int i = e.RowIndex + e.RowCount; i < dgv.Rows.Count; i++)
            {
                dgv.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        // 删除行时 重置行号
        private void dgvTask_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            var dgv = (DataGridView) sender;
            for (int i = 0; i < e.RowCount && i < dgv.Rows.Count; i++)
            {
                dgv.Rows[e.RowIndex + i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Rows[e.RowIndex + i].HeaderCell.Value = (e.RowIndex + i + 1).ToString();
            }
            for (int i = e.RowIndex + e.RowCount; i < dgv.Rows.Count; i++)
            {
                dgv.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgv.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        #endregion

        // 从DataGridView获取修改后的数据，用于保存
        private List<TaskItem> GetGridData()
        {
            List<TaskItem> save = new List<TaskItem>();

            // 用于判断是否存在相同路径，有相同路径的不允许保存
            Dictionary<string, int> arrpaths = new Dictionary<string, int>();
            foreach (DataGridViewRow row in dgvTask.Rows)
            {
                // 最后一行是新行，不用处理
                if (row.Index + 1 == dgvTask.Rows.Count)
                    continue;
                TaskItem item = GetRowData(row, arrpaths);
                if (item == null)
                    return null;
                save.Add(item);
            }
            return save;
        }

        TaskItem GetRowData(DataGridViewRow row, Dictionary<string, int> arrpaths = null)
        {
            string tmp = Convert.ToString(row.Cells[COLIDX_ID].Value);
            int id = string.IsNullOrEmpty(tmp) ? 0 : int.Parse(tmp);
            string taskdesc = Convert.ToString(row.Cells[COLIDX_DESC].Value).Trim().ToLower();
            string exepath = Convert.ToString(row.Cells[COLIDX_EXEPATH].Value).Trim().ToLower();
            string exepara = Convert.ToString(row.Cells[COLIDX_EXEPARA].Value).Trim();
            string typepara = Convert.ToString(row.Cells[COLIDX_TYPEPARA].Value).Trim();
            string runtypeStr = Convert.ToString(row.Cells[COLIDX_TYPE].Value).Trim();
            RunType runtype = (from pair in GetRunTypeDesc() where pair.Value == runtypeStr 
                               select (RunType) pair.Key).FirstOrDefault();

            #region 错误判断
            if (string.IsNullOrEmpty(exepath))
            {
                MessageBox.Show("第" + (row.Index + 1) + "行路径为空");
                return null;
            }
            // 防止出现 c:\\\\a.exe 或 c:/a.exe这样的路径,统一格式化成：c:\a.exe形式
            try
            {
                exepath = Path.GetDirectoryName(exepath) + @"\" + Path.GetFileName(exepath);
            }
            catch (Exception exp)
            {
                MessageBox.Show("第" + (row.Index + 1) + "行路径设置有误:" + exp.Message);
                return null;
            }
            if (!isSocketAccess && chkCheckFile.Checked && !File.Exists(exepath))
            {
                MessageBox.Show("第" + (row.Index + 1) + "行文件不存在:" + exepath);
                return null;
            }
            if (arrpaths != null && arrpaths.ContainsKey(exepath))
            {
                MessageBox.Show("第" + arrpaths[exepath] + "行与第" + (row.Index + 1) + "行路径设置重复，一个路径只能设置一次（文件名允许重复，但是完整路径不能重复）");
                return null;
            }
            if (runtype == RunType.PerDay || runtype == RunType.PerWeek || runtype == RunType.PerMonth)
            {
                if (string.IsNullOrEmpty(typepara))
                {
                    MessageBox.Show("第" + (row.Index + 1) + "行未设置任务参数:必须指定几点运行");
                    return null;
                }
            }
            #endregion

            if (arrpaths != null)
                arrpaths.Add(exepath, row.Index + 1);

            TaskItem item = new TaskItem
            {
                id = id,
                exepath = exepath,
                exepara = exepara,
                runtype = runtype,
                runcount = 0,
                desc = taskdesc,
                taskpara = typepara
            };
            return item;
        }

        // 从数据库获取数据，并绑定到DataGridView
        private bool GetTaskToView(DataGridView dgv, List<TaskItem> alltask = null)
        {
            try
            {
                dgv.Rows.Clear();

                if (alltask == null)
                    alltask = dbaccess.GetAllTask();
                if(alltask == null || alltask.Count == 0)
                {
                    return true;
                }

                // 把所有任务绑定到DataGridView
                foreach (TaskItem item in alltask)
                {
                    object[] colValues = new object[COL_NUM];
                    colValues[COLIDX_ID] = item.id;
                    colValues[COLIDX_DESC] = item.desc;
                    colValues[COLIDX_EXEPATH] = item.exepath.Trim();
                    colValues[COLIDX_EXEPARA] = item.exepara.Trim();
                    colValues[COLIDX_TYPE] = null;
                    colValues[COLIDX_TYPEPARA] = item.taskpara;
                    colValues[COLIDX_RUNCOUNT] = item.runcount;
                    colValues[COLIDX_PID] = item.pid;
                    colValues[COLIDX_PIDTIME] = item.pidtime.ToString("yyyy-MM-dd HH:mm:ss");
                    colValues[COLIDX_INSTIME] = item.instime.ToString("yyyy-MM-dd HH:mm:ss");
                    colValues[COLIDX_STATUS] = item.status;
                    colValues[COLIDX_DEL] = "删除";
                    colValues[COLIDX_SAVE] = "保存";
                    int i = dgv.Rows.Add(colValues);
                    DataGridViewRow row = dgv.Rows[i];

                    // 设置隔行背景色
                    SetBackColor(row, false);
                    // 绑定下拉框列表
                    BindDataGridViewComboBoxOption(row.Cells[COLIDX_TYPE], item.runtype);

                    if (item.status == ExeStatus.Running)
                        row.Cells[COLIDX_STATUS].Style.ForeColor = Color.Red;
                }
                return true;
            }
            catch (DbException exp)
            {
                if (exp.Message.StartsWith("File opened that is not a database file"))
                    MessageBox.Show("选择的不是正确的数据库文件，请重新选择");
                else
                    MessageBox.Show("打开数据库文件错误：" + exp.Message);
                return false;
            }
            catch(IndexOutOfRangeException)
            {
                MessageBox.Show("数据库表结构有误，请在sql窗口用下面的sql确认表结构\r\nselect * from sqlite_master");
                return false;
            }
            catch(Exception exp)
            {
                MessageBox.Show("其它错误：" + exp.Message);
                return false;
            }
        }

        /// <summary>
        /// 判断sql是否Select语句
        /// </summary>
        readonly static Regex regNotSelect = new Regex(@"(?:\s|^)(?:delete|update|insert|create|alter)\s", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private void btnRunSql_Click(object sender, EventArgs e)
        {
            if(dbaccess == null)
            {
                MessageBox.Show("请先选择数据库");
                return;
            }
            string sql = txtSql.Text;
            if(regNotSelect.IsMatch(sql))
            {
                // 不是Select语句，返回影响行数
                int rownum = dbaccess.ExecuteSql(sql);
                label1.Text = string.Format("{0}行受影响", rownum);
            }
            else
            {
                // Select语句，返回记录集
                DataSet ds = dbaccess.ExecuteDataset(sql);
                if (ds.Tables.Count > 0)
                {
                    dgvSqlData.DataSource = ds.Tables[0];
                    label1.Text = string.Format("{0}行 返回", ds.Tables[0].Rows.Count);
                }
            }
        }

        private void txtSql_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            #region Ctrl+按键
            if (e.Control)
            {
                // Ctrl+S保存
                if (e.KeyCode == Keys.S && tabControl1.SelectedIndex == 0)
                {
                    btnSaveToDb_Click(null, null);
                }
            }
            #endregion

            // F5执行Sql
            if(tabControl1.SelectedIndex == 1 && e.KeyCode == Keys.F5)
            {
                btnRunSql_Click(null, null);
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            new Help().ShowDialog(this);
        }

        Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Gray };
        private int colorIdx = 0;//new Random(Guid.NewGuid().GetHashCode()).Next(0, 3);
        void SetSocketStatus(string msg)
        {
            colorIdx++;
            if (colorIdx >= colors.Length)
                colorIdx = 0;
            labSocket.Text = msg;// +" " + DateTime.Now.ToString();
            labSocket.ForeColor = colors[colorIdx];
        }

        private static readonly Dictionary<int, string> _runtypeDesc = new Dictionary<int, string>();
        Dictionary<int, string> GetRunTypeDesc()
        {
            if (_runtypeDesc.Count <= 0)
            {
                Type type = typeof (RunType);
                Type typeDesc = typeof (DescriptionAttribute);
                foreach (int item in Enum.GetValues(type))
                {
                    string name = ((RunType) item).ToString();
                    FieldInfo fielInfo = type.GetField(name);
                    object[] objs = fielInfo.GetCustomAttributes(typeDesc, true);
                    string desc;
                    if (objs.Length <= 0)
                        desc =name;
                    else
                        desc = ((DescriptionAttribute)objs[0]).Description;
                    _runtypeDesc.Add(item, desc);
                }
            }
            return _runtypeDesc;
        }



        // 通过Socket连接服务器，获取全部任务
        private void btnSend_Click(object sender, EventArgs e)
        {
            // 连接网络数据库，返回全部任务
            string err;
            var tasks = TaskClient.GetAllTask(txtServerIP.Text, int.Parse(txtPort.Text), out err);
            if (tasks == null)
            {
                MessageBox.Show(err);
                return;
            }
            if (tasks.tasks != null)
            {
                GetTaskToView(dgvTask, tasks.tasks);
                isSocketAccess = true;
                string lastRunTime = "\r\n服务器的时间:" + tasks.serverTime + "\r\n上次轮询时间:" + tasks.lastRunTime;
                SetSocketStatus("连接成功 " + lastRunTime);
            }
        }
    }
}
