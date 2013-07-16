using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BO.DotPgDiff
{
    public partial class windowSql : Form
    {
        public windowSql()
        {
            InitializeComponent();
        }

        public void setText(string text)
        {
            this.textSql.Text = text;
        }

        public string getText()
        {
            return this.textSql.Text;

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            MessageBox.Show("avanti");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("stop");
        }
    }
}
