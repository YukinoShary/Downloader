using Downloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloaderWinForm
{
    public partial class FormTask : Form
    {
        public FormTask()
        {
            InitializeComponent();
        }

        public event Action<FormTask> ResultCallback;

        public DownloadItem Item;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Item = new DownloadItem(textBoxFrom.Text, textBoxFile.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error creating task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ResultCallback?.Invoke(this);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                textBoxFile.Text = sfd.FileName;
            }
        }
    }
}
