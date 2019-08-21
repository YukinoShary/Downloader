using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Downloader
{
    public class Download_Util
    {
        private Uri downloadUrl;
        private long blockSize;
        private int thread;//获取配置文件中的最大线程数
        private long cacheSize;//获取配置文件中缓冲大小
        //设置请求头部分信息
        private string UserAgent;
        private string AcceptEncoding;
        private List<Task> tasks = new List<Task>();//任务线程列表
        private string fileName;
        private long fileSize;     
        private long totalRange;       
        private long rangeSize;
        private long[] cutPosition;//记录不同块分段位置  
        private long current = 0;//实际总进度
        private long speed;
        private DispatcherTimer timer;
        private CancellationTokenSource source;//任务取消控制信号

        public Download_Util(TaskInfo info)
        {
            downloadUrl = info.downloadUrl;
            blockSize = info.blockSize;
            thread = info.thread;
            cacheSize = info.cacheSize;
            UserAgent = info.UserAgent;
            fileName = info.fileName;
            totalRange = info.totalRange;
            fileSize = info.fileSize;
            rangeSize = info.rangeSize;
            cutPosition = info.cutPosition;
            timer = info.timer;
            source = info.source;
            current = info.current;
        }

        public Download_Util()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36";
            //thread = Conf.config.maxThread;
            thread = 4;
            cacheSize = 1024 * 1024 * 64;
            //cacheSize = Conf.config.buffer;
            AcceptEncoding = "identity";
        }
        
        /// <summary>
        /// 下载任务准备工作
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task DownloadTask(string url)
        {
            HttpClient client = new HttpClient();
            downloadUrl = new Uri(url);
            //设置请求头
            client.DefaultRequestHeaders.Add("User-Agent", @UserAgent);
            
            //获取响应报文头，以获取文件总大小
            try
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", AcceptEncoding);
                HttpRequestMessage rm = new HttpRequestMessage(HttpMethod.Get,downloadUrl);
                HttpResponseMessage response = await client.SendAsync(rm, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                fileName = response.Content.Headers.ContentDisposition.FileName;
                fileSize = response.Content.Headers.ContentLength.Value;

            }
            catch(NullReferenceException e)
            {
                string[] sp = url.ToString().Split('/');
                string fileName = sp[sp.Length - 1];
                Debug.Write(fileName);
            }
            catch(Exception e)
            {
                MainWindow.mw.Dispatcher.Invoke(() => { MessageBox.Show(e.Message, "error", MessageBoxButton.OK); });
            }           

            if(fileSize < cacheSize)//当文件太小时，分配较少线程并把缓存设置为最小
            {
                thread = 2;
                cacheSize = 64 * 1024 * 1024;
            }
            blockSize = fileSize / thread;       //计算分块大小
            rangeSize = cacheSize / thread;       //每个Range参数的大小

            cutPosition = new long[thread];
            long cut = 0;
            for(int i=0;i<=cutPosition.Length-1;i++)
            {
                cutPosition[i] = cut + blockSize;
                cut += blockSize;
            }
            totalRange = (blockSize / rangeSize + (blockSize % rangeSize > 0 ? 1 : 0)) * thread;//计算总range数

            //新对话框确认下载文件名以及路径
            MainWindow.mw.Dispatcher.Invoke(() => 
            {
                var dialog = new SaveFileDialog();
                dialog.AddExtension = true;
                dialog.FileName = fileName;
                bool? result = dialog.ShowDialog();
                if(result == true)
                {
                    fileName = dialog.FileName;
                }
            });

            /*Allocate buffer and the range of task to every thread*/ 
            try
            {
                HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode == HttpStatusCode.PartialContent)//判断是否支持分段下载
                {
                    FileOperating.CreateFile(fileName, fileSize, totalRange);//分配下载空间

                    //下载计时器开始运行
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += Timer_Tick;
                    timer.Start();

                    source = new CancellationTokenSource();//初始化取消源
                    if (blockSize > 1)
                    {
                        long begin = 0;
                        for (int i = 0; i <= thread - 1; i++)
                        {
                            Console.WriteLine("下载线程" + i + "启动");
                            //await Task.Delay(1000);
                            tasks.Add(Task.Run(async () =>
                            {
                                await FileDownloadAsync(begin, begin + rangeSize, i);
                            }, source.Token));
                            begin += blockSize;
                        }
                    }
                }
                else if (response.StatusCode.Equals(200))
                {
                    //单线程下载
                    MainWindow.mw.Dispatcher.Invoke(() => { MessageBox.Show(response.StatusCode.ToString() + "服务器不支持断点续传，将进行单线程下载"); });
                    /*重新分配文件信息*/
                    totalRange = 1;
                    rangeSize = fileSize;
                    cutPosition = new long[1];
                    cutPosition[0] = fileSize - 1;
                    FileOperating.CreateFile(fileName, fileSize, totalRange);

                    tasks.Add(Task.Run(async () => {await SingleThreadDownload(); },source.Token));
                }
                else
                    throw new HttpRequestException(response.ReasonPhrase);
            }
            catch(Exception e)
            {
                MainWindow.mw.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(e.Message);
                });
            }
        }

        /// <summary>
        /// 文件多线程下载
        /// </summary>
        /// <param name="firstPosition">下载(块)起始位置</param>
        /// <param name="lastPosition">下载(块)结束位置</param>
        /// <param name="taskNum">线程编号</param>
        /// <returns></returns>
        private async Task FileDownloadAsync(long firstPosition, long lastPosition, int taskNum)//单元测试时设置为public，实际需要设置private
        {
            HttpClient client = new HttpClient();
            string range = "";

            if (lastPosition == 0)
            {
                range = "bytes=" + firstPosition + "-";
            }
            else
            {
                range = "bytes=" + firstPosition + "-" + lastPosition;
            }

            //设置请求头
            client.DefaultRequestHeaders.Add("User-Agent", @UserAgent);
            client.DefaultRequestHeaders.Add("Range", range);
            long rangeNum = (firstPosition + blockSize - cutPosition[taskNum]) / rangeSize - 1
                    + ((firstPosition + blockSize - cutPosition[taskNum]) % rangeSize > 0 ? 1 : 0)
                    + totalRange / thread * taskNum;
            /*rangeNum的计算为（定位该range在该block中的位置，即初始位置加上blockSize再减去该块末分段位置）
            除以每个进度块大小,最后加上之前的range数量*/

            HttpResponseMessage resp = await client.GetAsync(downloadUrl);
            if (resp.StatusCode == HttpStatusCode.PartialContent)
            {
                using (Stream stream = await resp.Content.ReadAsStreamAsync())
                {
                    //缓存写入硬盘
                    FileOperating.SaveFile(fileName, stream, firstPosition, rangeNum);
                }
            }
            else
                throw new HttpRequestException("got http status code " + resp.StatusCode + ", expected 206.");

            //当线程任务未超过block范围，继续下一个Range任务
            if (firstPosition + rangeSize < cutPosition[taskNum])
            {
                await FileDownloadAsync(lastPosition + 1, lastPosition + rangeSize, taskNum);
            }
            else if (lastPosition < taskNum)//末尾有余留内容时
            {
                await FileDownloadAsync(lastPosition + 1, cutPosition[taskNum], taskNum);
            }
            else 
            {
                FileOperating.StatusUpdate(fileName);
            }
            return;
        }

        /// <summary>
        /// 单线程下载任务
        /// </summary>
        /// <returns></returns>
        private async Task SingleThreadDownload()
        {
            HttpClient client = new HttpClient();
            Stream stream = await client.GetStreamAsync(downloadUrl);
            FileOperating.SaveFile(fileName, stream, 0, fileSize - 1);
        }

        /// <summary>
        /// 暂停下载任务并释放资源,返回值为该暂停任务的info
        /// </summary>
        public TaskInfo Pause()
        {
            timer.Stop();
            source.Cancel();
            for(int i=0; i<=tasks.Count-1;i++)
            {
                tasks[i].Dispose();
            }
            tasks.Clear();
            current = ProgressCalculate(FileOperating.GetProgress(fileName));//获取暂停前的实时进度
            return SaveInfomation();
        }

        /// <summary>
        /// 继续下载任务
        /// </summary>
        public async Task ContinueTask()
        {
            long[] scanner = FileOperating.GetProgress(fileName); //获取下载进度
            int taskcount = 0;

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            //判断是否可以进行分段下载
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.PartialContent)
            {
                for (int i = 0; i <= scanner.Length - 1; i++)
                {
                    if (scanner[i] <= rangeSize && (i + 1) % (totalRange / thread) != 0)//判断每个range内容长度，如果长度小于rangeSize且它不是block末尾range则从此处继续下载
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await FileDownloadAsync(i * rangeSize + scanner[i], i * scanner[i] + rangeSize, taskcount);//非末尾range
                        }, source.Token));
                        taskcount += 1;
                        i = (int)totalRange / thread * taskcount - 1;//直接跳转到下一个block开始位置
                    }
                    else if ((i + 1) % (totalRange / thread) == 0)//当该range为分块末尾range时
                    {
                        if (scanner[i] < blockSize - (totalRange / thread - 1) * rangeSize)//判断该末尾range是否小于预定值
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                await FileDownloadAsync(i * rangeSize + scanner[i], cutPosition[taskcount], taskcount);//末尾range
                            }, source.Token));
                            taskcount += 1;
                            i = (int)totalRange / thread * taskcount - 1;
                        }
                    }
                }
            }
            else
                MainWindow.mw.Dispatcher.Invoke(() => { MessageBox.Show(response.StatusCode.ToString() + "无法继续进行下载"); });
        }

        /// <summary>
        /// 下载任务计时器，负责更新下载进度信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, object e)
        {
            long lastProgress = current;
            speed = (current - lastProgress) / 2;

            current = ProgressCalculate(FileOperating.GetProgress(fileName));
            Debug.WriteLine(current);
            DownloadTasksPage.dtp.UpdateProgress(fileName, current, fileSize);
            Debug.WriteLine(speed);
            DownloadTasksPage.dtp.UpdatePercent(fileName, (current / fileSize));
            DownloadTasksPage.dtp.UpdateSpeed(fileName, speed);

            if(current==fileSize)
            {
                TaskFinish();
                FileOperating.SaveProgress();
            }
        }

        /// <summary>
        /// 计算实际下载完成的数据量
        /// </summary>
        /// <param name="a">进度记录，每一个元素的内容是range内实际存储文件大小</param>
        /// <returns></returns>
        private long ProgressCalculate(long[] a)
        {
            long current = 0;
            for(long i=0;i<=a.Length-1;i++)
            {
                current += a[i];              
            }
            return current;
        }

        public string getFileName()
        {
            return fileName;
        }

        private void TaskFinish()
        {
            tasks.Clear();
            DownloadTasksPage.dtp.TaskFinished(fileName);
        }

        private TaskInfo SaveInfomation()
        {
            TaskInfo ti = new TaskInfo();
            ti.downloadUrl = downloadUrl;
            ti.blockSize = blockSize;
            ti.thread = thread;
            ti.cacheSize = cacheSize;
            ti.UserAgent = UserAgent;
            ti.fileName = fileName;
            ti.totalRange = totalRange;
            ti.fileSize = fileSize;
            ti.rangeSize = rangeSize;
            ti.cutPosition = cutPosition;
            ti.timer = timer;
            ti.source = source;
            return ti;
        }
    }
}
