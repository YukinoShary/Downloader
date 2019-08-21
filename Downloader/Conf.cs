using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class Conf
    {
        public string storagePath { get; set; }
        public long buffer { get; set; }
        public int maxThread{get;set;}
        public string infoPath { get; set; }
        public static Conf config;
        private static string ConfigLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "conf.json";
        public static void getConf()
        {
            try
            {
                config = LoadConf(ConfigLocation);
            }   
            catch(FileNotFoundException)
            {
                config = Default();
                SaveConf();
            }
        }

        /// <summary>
        /// 默认配置
        /// </summary>
        /// <returns></returns>
        private static Conf Default()
        {
            Conf c = new Conf();
            //默认配置设定 .....
            c.buffer = 128 * 1024 * 1024;
            c.maxThread = 4;
            //获取系统环境文件夹
            c.infoPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "info.inf";
            c.storagePath = "D:\\doltest";
            return c;
        }

        /// <summary>
        /// 供外部调用的保存配置文件方法
        /// </summary>
        public static void SaveConf()
        {
            string json = JsonConvert.SerializeObject(config);
            File.WriteAllText(ConfigLocation, json);
        }


        /// <summary>
        /// 读取磁盘中的信息文件
        /// </summary>
        /// <returns>返回Conf类型文件</returns>
        public static Conf LoadConf(string path)
        {
            string sourceJson = File.ReadAllText(path);
            try
            {
                return (Conf)JsonConvert.DeserializeObject(sourceJson);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }
}
