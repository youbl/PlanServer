using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PlanServerTaskManager
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            if (Owner != null)
            {
                Left = Owner.Left + 20;
                Top = Owner.Top + Owner.Height - Height - 20;
                if (Top < 0)
                    Top = 0;
            }
            base.OnLoad(e);
        }
    }
}
