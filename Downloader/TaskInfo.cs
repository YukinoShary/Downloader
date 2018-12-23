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
    [Serializable]
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
        public DispatcherTimer timer { get; set; }
        public CancellationTokenSource source { get; set; }

        public static List<TaskInfo> Li { get; set; }//硬盘中存储的上一次关闭时的任务列表
        public static void InfoDeserialize(byte[] b)
        {
            using (MemoryStream ms = new MemoryStream(b))
            {
                IFormatter formatter = new BinaryFormatter();
                Li = (List<TaskInfo>)formatter.Deserialize(ms);
            }
        }
        public static Stream InfoSerialize()
        {
            MemoryStream ms = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, Li);
            return ms;
        }

        /// <summary>
        /// 获取任务列表
        /// </summary>
        public static void getTaskInfo()
        {
            try
            {
                InfoDeserialize(FileOperating_Util.LoadInfo(Conf.config.infoPath));
            }
            catch(FileNotFoundException)
            {
                Li = null;
                return;
            }
        }
    }
}
