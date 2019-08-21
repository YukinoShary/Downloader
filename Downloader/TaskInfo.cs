using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Downloader
{
    public class TaskInfo
    {
        public Uri downloadUrl { get; set; }
        public long blockSize { get; set; }
        public int thread { get; set; }
        public long cacheSize { get; set; }
        public string UserAgent { get; set; }
        public string fileName { get; set; }
        public long fileSize { get; set; }
        public long totalRange { get; set; }
        public long rangeSize { get; set; }
        public long[] cutPosition { get; set; }
        public long current { get; set; }
        public DispatcherTimer timer { get; set; }
        public CancellationTokenSource source { get; set; }
       
    }
}
