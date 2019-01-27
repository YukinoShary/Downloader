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
    public class DownloadItem
    {
        public string Source;
        public string DestFilePath;
        public FileDownloader FileDownloader;
        public ListViewItem ListViewItem;
        public string StateFilePath;

        public DownloadItem(string source, string dest)
        {
            Source = source;
            DestFilePath = dest;
            FileDownloader = new FileDownloader();
            FileDownloader.SourceUri = new Uri(source);
            FileDownloader.DestFilePath = dest;
            StateFilePath = dest + ".downloading";
        }

        public void TryRestoreState()
        {
            if (File.Exists(StateFilePath))
            {
                FileDownloader.SetState(File.ReadAllText(StateFilePath, Encoding.UTF8));
            }
        }

        public void InitView(ListView lv)
        {
            ListViewItem = new ListViewItem();
            ListViewItem.Tag = this;
            ListViewItem.Text = FileDownloader.DestFilePath;
            ListViewItem.SubItems.AddRange(new string[] { "- / -", "-" });
            lv.Items.Add(ListViewItem);
        }

        int lastTick;
        long lastDl;

        public void UpdateView()
        {
            ListViewItem.SubItems[1].Text = FileDownloader.Downloaded + " / " + FileDownloader.Length;
            var curTick = Environment.TickCount; // TODO: this can overflow
            var curDl = FileDownloader.Downloaded;
            if (lastTick != 0)
            {
                var deltaTick = curTick - lastTick;
                var deltaDl = curDl - lastDl;
                ListViewItem.SubItems[2].Text = (deltaDl * 1000 / deltaTick / 1024) + " KB/s";
            }
            lastTick = curTick;
            lastDl = curDl;
        }
    }
}
