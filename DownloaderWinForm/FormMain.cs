using Downloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloaderWinForm
{
    public partial class FormMain : Form
    {
        const string TaskListFile = "tasks.conf";

        static readonly UTF8Encoding UTF8WithoutBOM = new UTF8Encoding(false);

        Timer timer;

        List<DownloadItem> tasks = new List<DownloadItem>();

        public FormMain()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            timer = new Timer();
            timer.Interval = 1000;
            timer.Enabled = true;
            timer.Tick += Timer_Tick;
            ReadTaskList();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var item in tasks)
            {
                item.UpdateView();
                var state = item.FileDownloader.CheckAutoSave();
                if (state != null)
                {
                    File.WriteAllText(item.StateFilePath, state, UTF8WithoutBOM);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var formTask = new FormTask();
            formTask.Show(this);
            formTask.ResultCallback += (x) =>
            {
                var item = x.Item;
                AddItem(item);
                SaveTaskList();
            };
        }

        private void AddItem(DownloadItem item)
        {
            item.TryRestoreState();
            tasks.Add(item);
            item.InitView(listView);
            item.UpdateView();
            Task.Run(() => item.FileDownloader.Start());
        }

        private void SaveTaskList()
        {
            var sb = new StringBuilder();
            foreach (var item in tasks)
            {
                sb.AppendLine(item.Source);
                sb.AppendLine(item.DestFilePath);
                sb.AppendLine();
            }
            File.WriteAllText(TaskListFile, sb.ToString(), UTF8WithoutBOM);
        }

        private void ReadTaskList()
        {
            if (!File.Exists(TaskListFile))
                return;
            var text = File.ReadAllText(TaskListFile, Encoding.UTF8);
            var sr = new StringReader(text);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var source = line;
                var dest = sr.ReadLine();
                AddItem(new DownloadItem(source, dest));
                while (sr.ReadLine().Length > 0) { }
            }
        }
    }
}
