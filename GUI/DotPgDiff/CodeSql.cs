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
    public partial class CodeSql : Form
    {
        public CodeSql(string SQL)
        {
            InitializeComponent();
			this.textSql.Text = SQL;
        }


		public string SQL
		{
			get
			{
				return this.textSql.Text;
			}
		}

        private void btnOk_Click(object sender, EventArgs e)
        {
			this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
			this.DialogResult = DialogResult.Abort;
        }
    }
}
