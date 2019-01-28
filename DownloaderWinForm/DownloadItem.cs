using Downloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public CancellationTokenSource cts;
        public string StateFilePath;

        public DownloadItem(string source, string dest)
        {
            Source = source;
            DestFilePath = dest;
            FileDownloader = new FileDownloader
            {
                SourceUri = new Uri(source),
                DestFilePath = dest
            };
            StateFilePath = dest + ".downloading";
        }

        public void Start()
        {
            if (FileDownloader.IsRunning)
                return;
            cts = new CancellationTokenSource();
            FileDownloader.CancellationToken = cts.Token;
            FileDownloader.Start();
        }

        public void Pause()
        {
            cts?.Cancel();
        }

        public void TryRestoreState()
        {
            if (File.Exists(StateFilePath))
            {
                FileDownloader.SetState(Utils.GetText(StateFilePath));
            }
        }

        public void InitView(ListView lv)
        {
            ListViewItem = new ListViewItem();
            ListViewItem.Tag = this;
            ListViewItem.Text = FileDownloader.DestFilePath;
            ListViewItem.SubItems.AddRange(new string[] { "- / -", "", "", "" });
            lv.Items.Add(ListViewItem);
        }

        public void RemoveView()
        {
            ListViewItem.Remove();
        }

        int lastTick;
        long lastDl;

        public void UpdateView()
        {
            ListViewItem.SubItems[1].Text = FormatSize(FileDownloader.Downloaded) + " / "
                + (FileDownloader.Length < 0 ? "?" : FormatSize(FileDownloader.Length));
            var curTick = Environment.TickCount; // TODO: this can overflow
            var curDl = FileDownloader.Downloaded;
            if (FileDownloader.State != DownloadState.Downloading)
            {
                ListViewItem.SubItems[2].Text = "(" + FileDownloader.State + ")";
            }
            else if (lastTick != 0)
            {
                var deltaTick = curTick - lastTick;
                var deltaDl = curDl - lastDl;
                ListViewItem.SubItems[2].Text = FormatSpeed(deltaDl, deltaTick);
            }
            lastTick = curTick;
            lastDl = curDl;
            ListViewItem.SubItems[3].Text = FileDownloader.DownloadingThreads.ToString();
            ListViewItem.SubItems[4].Text = FileDownloader.Errors.ToString();
        }

        static readonly string[] Units = new string[] { " B", " KB", " MB", " GB", " TB" };

        static string FormatSize(long bytes)
        {
            int unit = 0;
            float num = (float)bytes;
            while (num >= 1024 && unit + 1 < Units.Length)
            {
                num /= 1024;
                unit++;
            }
            if (unit == 0)
                return num.ToString("N0") + Units[0];
            else
                return num.ToString("N1") + Units[unit];
        }

        static string FormatSpeed(long deltaBytes, int deltaMs)
        {
            return (deltaBytes * 1000 / deltaMs / 1024).ToString("N0") + " KB/s";
        }
    }
}
