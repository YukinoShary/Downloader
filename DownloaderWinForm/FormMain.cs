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
        Timer timer;
        DownloadManager dm;

        public FormMain()
        {
            InitializeComponent();
            InitContextMenu();
        }

        Action updateMenuState;

        private void InitContextMenu()
        {
            var menu = new ContextMenuStrip();
            ToolStripItem pause = menu.Items.Add("&Pause Selected");
            pause.Click += (s, e) =>
            {
                foreach (ListViewItem item in listView.SelectedItems)
                {
                    var task = (DownloadItem)item.Tag;
                    task.Pause();
                }
            };
            ToolStripItem start = menu.Items.Add("&Start Selected");
            start.Click += (s, e) =>
            {
                foreach (ListViewItem item in listView.SelectedItems)
                {
                    var task = (DownloadItem)item.Tag;
                    if (!task.FileDownloader.IsRunning)
                        task.Start();
                }
            };
            menu.Items.Add(new ToolStripSeparator());
            ToolStripItem remove = menu.Items.Add("&Remove Selected");
            remove.Click += (s, e) =>
            {
                foreach (ListViewItem item in listView.SelectedItems)
                {
                    var task = (DownloadItem)item.Tag;
                    dm.RemoveItem(task);
                }
                dm.SaveTaskList();
            };
            listView.ContextMenuStrip = menu;
            updateMenuState = () =>
            {
                pause.Enabled = start.Enabled = remove.Enabled = false;
                if (listView.SelectedItems.Count > 0)
                {
                    remove.Enabled = true;
                    foreach (ListViewItem item in listView.SelectedItems)
                    {
                        var task = (DownloadItem)item.Tag;
                        if (task.FileDownloader.IsRunning)
                            pause.Enabled = true;
                        else
                            start.Enabled = true;
                    }
                }
            };
            listView.SelectedIndexChanged += (s, e) => updateMenuState();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dm = new DownloadManager() { listView = this.listView };
            dm.ReadTaskList();

            timer = new Timer { Interval = 1000, Enabled = true };
            timer.Tick += Timer_Tick;
        }

        int timerTicks;

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (++timerTicks == 3)
            {
                timerTicks = 0;
                dm.CheckPoint();
            }
            foreach (var item in dm.tasks)
            {
                item.UpdateView();
            }
            updateMenuState();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var formTask = new FormTask();
            formTask.ResultCallback += (x) =>
            {
                var item = x.Item;
                dm.AddItem(item);
                dm.SaveTaskList();
            };
            formTask.ShowDialog(this);
        }
    }
}
