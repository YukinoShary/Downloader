using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloaderWinForm
{
    class DownloadManager
    {
        const string TaskListFile = "tasks.conf";

        public List<DownloadItem> tasks = new List<DownloadItem>();
        public ListView listView;

        public void AddItem(DownloadItem item)
        {
            item.TryRestoreState();
            tasks.Add(item);
            item.InitView(listView);
            item.UpdateView();
            item.Start();
        }

        public void RemoveItem(DownloadItem item)
        {
            tasks.Remove(item);
            item.RemoveView();
            Task.Run(() => item.Pause());
        }

        public void SaveTaskList()
        {
            var sb = new StringBuilder();
            foreach (var item in tasks)
            {
                sb.AppendLine(item.Source);
                sb.AppendLine(item.DestFilePath);
                sb.AppendLine();
            }
            Utils.SaveTextAtomic(TaskListFile, sb.ToString());
        }

        public void ReadTaskList()
        {
            if (!File.Exists(TaskListFile))
                return;
            var text = Utils.GetText(TaskListFile);
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

        public void CheckPoint()
        {
            foreach (var item in tasks)
            {
                var state = item.FileDownloader.CheckAutoSave();
                if (state != null)
                {
                    Utils.SaveTextAtomic(item.StateFilePath, state);
                }
            }
        }
    }
}
