using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Downloader
{
    public class FileOperating_Util
    {
        //https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-setendoffile
        //https://blogs.msdn.microsoft.com/oldnewthing/20150710-00/?p=45171
        //https://msdn.microsoft.com/zh-cn/library/aa686045.aspx
        [DllImport("Kernel32.dll", EntryPoint ="SetEndOfFile")]
        private static extern bool SetEndOfFile(IntPtr hFile);   

        public static string path;                                  //获取设置的下载路径
        private static Dictionary<string,FileStream> fs = new Dictionary<string,FileStream>();
        private static Object locker = new Object();                  //建立进程锁对象
        private static Dictionary<string, long[]> progress=new Dictionary<string, long[]>();    //初始化静态任务进度表

        public FileOperating_Util()
        {
            progress = ProgressDeserialize(LoadInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "progress.prg"));
        }

        public static Stream ProgressSerialize()
        {
            MemoryStream ms = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, progress);
            return ms;
        }

        private static Dictionary<string, long[]> ProgressDeserialize(byte[] b)
        {
            using (MemoryStream ms = new MemoryStream(b))
            {
                IFormatter formatter = new BinaryFormatter();
                return (Dictionary<string, long[]>)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// 将信息文件存储在本地
        /// </summary>
        /// <param name="str"></param>
        public static void SaveInfo(Stream sourceStream,string path)
        {
            using (FileStream fileStream = new FileStream(path,FileMode.Create,FileAccess.Write))
            {
                sourceStream.CopyTo(fileStream);
                sourceStream.Dispose();
            }
        }

        /// <summary>
        /// 读取磁盘中的信息文件
        /// </summary>
        /// <returns>返回Conf类型文件</returns>
        public static byte[] LoadInfo(string path)
        {
            using (FileStream fileStream = new FileStream(path,FileMode.Open,FileAccess.Read))
            {
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }

        /// <summary>
        /// 创建新文件对象，为下载内容设置硬盘空间
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="size">文件总大小</param>
        /// /// <param name="blockRanges">每个block中的range数量</param>
        public static void CreateFile(string fileName,long size,long Ranges)
        {
            if (File.Exists(path + fileName))//当不为续写模式但是文件已存在,由用户判断后续操作
            {
                //主线程invoke           
                MessageBoxResult result = MessageBox.Show("该目录下已存在该文件，是否重新下载？", "Warning", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    CreateFile(fileName + "(new)", size, Ranges);
                else if (result == MessageBoxResult.No)
                    return;
            }
            else
            {
                progress.Add(fileName, new long[Ranges]);//建立新的下载进度,每一个元素表示各个range的长度
                try
                {
                    fs.Add(fileName, new FileStream(path + fileName, FileMode.Create, FileAccess.Write));
                    //为下载内容设置硬盘空间 
                    if (SetEndOfFile(fs[fileName].Handle))
                        return;
                    else
                        throw new IOException();
                }
                catch (Exception e)
                {
                    MainWindow.mw.Dispatcher.Invoke(() => { MessageBox.Show(e.Message); });
                }
            }

        }

        /// <summary>
        /// 缓冲块写入,文件续写
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="block"></param>
        /// <param name="rangeNum"></param>
        /// <returns></returns>
        public static bool SaveFile(string fileName,Stream result,long position,long rangeNum)
        {
            using (FileStream fs = new FileStream(path + fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                
            }
            try
            {
                byte[] buf = new byte[4096];
                long count = 0;
                long total = result.Length / 4096;
                while (count <= total)
                {
                    result.Read(buf, 0, buf.Length);
                    lock (locker)
                    {
                        fs[fileName].Seek(position, SeekOrigin.Begin);//在文件流中查找range位置
                        fs[fileName].Write(buf, 0, buf.Length);
                        count += 1;
                        progress[fileName][rangeNum] += 4096;//记录存储进度
                        position += buf.Length;
                    }
                }
                long remainder = result.Length % 4096;
                //剩余内容写入
                if (remainder != 0)
                {
                    byte[] remainBuf = new byte[remainder];
                    result.Read(remainBuf, 0, remainBuf.Length);
                    lock (locker)
                    {
                        fs[fileName].Seek(position, SeekOrigin.Begin);
                        fs[fileName].Write(remainBuf, 0, remainBuf.Length);
                        progress[fileName][rangeNum] += remainder;
                        position += remainder;
                    }

                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 删除文件操作
        /// </summary>
        /// <param name="filename"></param>
        public static void DeleteFile(string filename)
        {
            lock(locker)
            {
                if(File.Exists(path+filename))
                {
                    try
                    {
                        File.Delete(path + filename);
                    }
                    catch(IOException e)
                    {
                        MessageBox.Show(e.Message, "IOException");
                    }
                }
            }
        }

        /// <summary>
        /// 返回单个任务的分块进度条
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static long[] getProgress(string filename)
        {
            return progress[filename];
        }
    }
}
