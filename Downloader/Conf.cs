using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

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
        private static string ConfigLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "conf.cfg";
        public static void getConf()
        {
            try
            {
                byte[] b = FileOperating_Util.LoadInfo(ConfigLocation);
                if (b!=null&&b.Length!=0)
                {
                    config = ConfDeserialize(b);
                }
                else
                {
                    config = Default();
                    SaveConf();
                }

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
        /// 序列化配置文件
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static Stream ConfSerialize()
        {
            MemoryStream ms = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, config);
            return ms;
        }

        /// <summary>
        /// 反序列化conf文件
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private static Conf ConfDeserialize(byte[] b)
        {
            using (MemoryStream ms = new MemoryStream(b))
            {
                IFormatter formatter = new BinaryFormatter();
                return (Conf)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// 供外部调用的保存配置文件方法
        /// </summary>
        public static void SaveConf()
        {
            FileOperating_Util.SaveInfo(ConfSerialize(), ConfigLocation);
        }
    }
}
