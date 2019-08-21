using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Downloader
{
    [Serializable]
    class TaskList
    {
        public static Dictionary<string,TaskInfo> Li { get; set; }//硬盘中存储的上一次关闭时的任务列表
        public static void SaveInfo()
        {
            string json = JsonConvert.SerializeObject(Li);
            File.WriteAllText(Conf.config.infoPath, json);
        }

        /// <summary>
        /// 获取任务列表
        /// </summary>
        public static void getTaskInfo()
        {
            try
            {
                string sourceJson = File.ReadAllText(Conf.config.infoPath);
                Li = (Dictionary<string, TaskInfo>)JsonConvert.DeserializeObject(sourceJson);
            }
            catch (FileNotFoundException)
            {
                Li = null;
                return;
            }
        }
    }
}
