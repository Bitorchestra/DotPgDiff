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
    public partial class Connect : Form
    {
        public Connect()
        {
            InitializeComponent();
        }

        public Npgsql.NpgsqlConnectionStringBuilder ConnectionString
        {
            get
            {
                return new Npgsql.NpgsqlConnectionStringBuilder {
                    Host = this.host.Text,
                    Port = Int32.Parse(this.port.Text),
                    UserName = this.user.Text,
                    Password = this.password.Text,
                    Database = this.database.Text
                };
            }
        }

        private Exception TryConnect()
        {
            try {
                using (var c = new Query.Loader(this.ConnectionString))
                    return null;
            } catch (Exception e) {
                return e;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var rt = this.TryConnect();
            if (rt == null) {
                this.DialogResult = DialogResult.OK;
            } else {
                MessageBox.Show(rt.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
