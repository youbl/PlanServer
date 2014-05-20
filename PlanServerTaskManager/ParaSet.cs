using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PlanServerService;

namespace PlanServerTaskManager
{
    public partial class ParaSet : Form
    {
        private const int COL_NUM = 5;
        private const int COL_DAY = 1;
        private const int COL_TIME = 2;
        private const int COL_RUNMIN = 3;
        private const int COL_ENDTIME = 4;
        private const int COL_DEL = 0;

        private RunType m_type;
        private DataGridViewCell m_cell;

        public ParaSet(RunType type, DataGridViewCell cell)
        {
            InitializeComponent();
            ShowInTaskbar = false; // 不能放到OnLoad里，会导致窗体消失

            m_type = type;
            m_cell = cell;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Owner != null)
            {
                Left = Owner.Left + 20;
                Top = Owner.Top + 20;
                if (Left < 0)
                    Left = 0;
                if (Top < 0)
                    Top = 0;
            }

            // 初始化现有的参数值
            switch (m_type)
            {
                case RunType.PerDay:
                    colDay.Visible = false;
                    lstDays.Items.Add("每天");
                    lstDays.Enabled = false;
                    break;
                case RunType.PerWeek:
                    colDay.HeaderText = "周几运行(0为周日)";
                    lstDays.Items.AddRange(new object[] {"每周日", "每周一", "每周二", "每周三", "每周四", "每周五", "每周六"});
                    break;
                case RunType.PerMonth:
                    colDay.HeaderText = "每月几号运行";
                    for (var i = 1; i < 32; i++)
                        lstDays.Items.Add("每月" + i + "号");
                    break;
                default:
                    return;
            }
            lstDays.SelectedIndex = 0;
            txtTm.Text = DateTime.Now.ToString("HH:mm");

            string oldVal = Convert.ToString(m_cell.Value).Trim();
            txtResult.Text = oldVal;

            List<TimePara> paras = TaskItem.GetPara(m_type, oldVal);
            DataGridView dgv = dataGridView1;
            // 把所有任务绑定到DataGridView
            foreach (TimePara item in paras)
            {
                object[] colValues = new object[COL_NUM];
                colValues[COL_DAY] = item.WeekOrDay;
                colValues[COL_TIME] = item.StartHour.ToString("00") + ":" + item.StartMin.ToString("00");
                colValues[COL_RUNMIN] = item.RunMinute.ToString();
                colValues[COL_ENDTIME] = CountEndTime(item.StartHour, item.StartMin, item.RunMinute);
                colValues[COL_DEL] = "删除";
                int i = dgv.Rows.Add(colValues);
                DataGridViewRow row = dgv.Rows[i];

                // 设置隔行背景色
                SetBackColor(row, false);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string para = GetPara();
            if (para.Length == 0)
            {
                MessageBox.Show("没有输入参数");
                return;
            }
            m_cell.Value = para;
            btnCancel_Click(sender, e);
        }

        private string GetPara()
        {
            // 按日期时间排序
            IComparer aa = new DataGridViewRowComparer();
            dataGridView1.Sort(aa);

            StringBuilder sb = new StringBuilder();
            string lastrow = null;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string strrow = string.Empty;
                if (lstDays.Enabled)
                    strrow = row.Cells[COL_DAY].Value.ToString() + "-";
                strrow += row.Cells[COL_TIME].Value.ToString();

                if (lastrow != null && lastrow == strrow)
                {
                    MessageBox.Show("存在时间重复的参数");
                    return string.Empty;
                }
                lastrow = strrow;
                sb.AppendFormat(strrow + "|" + row.Cells[COL_RUNMIN].Value.ToString() + ";");
            }
            return sb.ToString();
        }


        /// <summary>
        /// 设置焦点行的背景色
        /// </summary>
        /// <param name="row"></param>
        /// <param name="isFocus"></param>
        private static void SetBackColor(DataGridViewRow row, bool isFocus)
        {
            Color color;
            if (isFocus)
                color = Color.YellowGreen;
            else if (row.Index%2 == 0)
                color = Color.White;
            else
                color = Color.FromArgb(233, 245, 178);

            row.DefaultCellStyle.BackColor = color;
            //foreach (DataGridViewCell cell in row.Cells)
            //{
            //    cell.Style.BackColor = color;
            //}
        }

        private void ParaSet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                btnCancel_Click(sender, e);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string tm = txtTm.Text;
            string[] tmparr = tm.Split(':');
            int hour, min;
            if (tmparr.Length != 2 ||
                !int.TryParse(tmparr[0], out hour) || hour < 0 || hour > 23 ||
                !int.TryParse(tmparr[1], out min) || min < 0 || min > 59)
            {
                MessageBox.Show("时间设置错误");
                return;
            }
            int runmin;
            if (!int.TryParse(txtRunMin.Text, out runmin))
            {
                MessageBox.Show("运行时长设置错误");
                return;
            }

            DataGridView dgv = dataGridView1;
            object[] colValues = new object[COL_NUM];
            if (m_type == RunType.PerMonth)
                colValues[COL_DAY] = lstDays.SelectedIndex + 1;
            else //if (m_type == RunType.PerWeek) 注释，便于DataGridViewRowComparer比较
                colValues[COL_DAY] = lstDays.SelectedIndex;

            colValues[COL_TIME] = hour.ToString("00") + ":" + min.ToString("00");
            colValues[COL_RUNMIN] = runmin.ToString();
            colValues[COL_ENDTIME] = CountEndTime(hour, min, runmin);
            colValues[COL_DEL] = "删除";
            int i = dgv.Rows.Add(colValues);
            DataGridViewRow row = dgv.Rows[i];

            // 设置隔行背景色
            SetBackColor(row, false);

            txtResult.Text = GetPara();
        }


        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == COL_DEL)
            {
                DataGridView dgv = dataGridView1;
                if (e.RowIndex < 0) // || e.RowIndex >= dgv.Rows.Count - 1)//Rows.Count - 1是不让点击未提交的新行
                    return;

                dgv.Rows.RemoveAt(e.RowIndex);
                txtResult.Text = GetPara();
                //return;
            }

        }


        private static string CountEndTime(int startHour, int startMin, int runMinute)
        {
            string endtime;
            if (runMinute <= 0)
                endtime = "不自动终止";
            else
            {
                // 计算运行时长
                var day = runMinute/1440; // 几天 每天是1440分钟=24*60
                var tmpMin = runMinute - day*1440;
                var hour = tmpMin/60; // 几小时
                var min = tmpMin - hour*60; // 几分

                endtime = "运行";
                if (day > 0)
                    endtime += day + "天";
                if (hour > 0)
                    endtime += hour + "小时";
                if (min > 0)
                    endtime += min + "分钟";

                // 计算具体结束时间
                min += startMin;
                if (min >= 60)
                {
                    min -= 60;
                    hour += 1;
                }
                hour += startHour;
                if (hour >= 24)
                {
                    hour -= 24;
                    day += 1;
                }
                endtime += ";将于";
                if (day == 0)
                    endtime += "当天";
                else if (day == 1)
                    endtime += "次日";
                else if (day > 1)
                    endtime += "第" + (day) + "天的";
                endtime += hour.ToString("00") + ":" + min.ToString("00") + "结束";
            }
            return endtime;
        }

        private class DataGridViewRowComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DataGridViewRow a = x as DataGridViewRow;
                if (a == null)
                    return -1;
                DataGridViewRow b = y as DataGridViewRow;
                if (b == null)
                    return 1;
                int ret = Convert.ToInt32(a.Cells[COL_DAY].Value).CompareTo(
                    Convert.ToInt32(b.Cells[COL_DAY].Value));
                if (ret == 0)
                    ret = String.Compare(Convert.ToString(a.Cells[COL_TIME].Value),
                                         Convert.ToString(b.Cells[COL_TIME].Value),
                                         StringComparison.Ordinal);
                return ret;
            }
        }
    }
}