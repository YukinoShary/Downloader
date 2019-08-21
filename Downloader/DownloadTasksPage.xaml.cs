using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace Downloader
{
    /// <summary>
    /// DownloadTasksPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadTasksPage : Page
    {
        public static DownloadTasksPage dtp;
        private int TaskCount = 0;
        private Dictionary<string, string> dataBinding;
        private Dictionary<string, Download_Util> tasks = new Dictionary<string, Download_Util>();

        public DownloadTasksPage()
        {
            InitializeComponent();
            dtp = this;
            dataBinding = new Dictionary<string, string>();
            CreateList();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NewTask nt = new NewTask();
            nt.Show();
        }

        /// <summary>
        /// 前台显示添加任务项
        /// </summary>
        /// <param name="filename"></param>
        private void NewTaskItem(string filename)
        {
            TaskCount += 1;
            DownloadItem item = new DownloadItem(filename);

            dataBinding.Add(filename + "currentDown", "");
            Binding processBinding = new Binding();
            processBinding.Source = dataBinding[filename + "currentDown"];
            BindingOperations.SetBinding(item.currentDone, TextBlock.TextProperty, processBinding);//数据绑定

            dataBinding.Add(filename + "pauseContinue", ">");
            Binding buttonBinding = new Binding();
            buttonBinding.Source = dataBinding[filename + "pauseContinue"];
            item.pauseContinue.Click += TaskPause;
            BindingOperations.SetBinding(item.pauseContinue, Button.ContentProperty, buttonBinding);

            dataBinding.Add(filename + "speed", "");
            Binding speedBinding = new Binding();
            speedBinding.Source = dataBinding[filename + "speed"];
            BindingOperations.SetBinding(item.speed, TextBlock.TextProperty, speedBinding);

            dataBinding.Add(filename + "percent", "");
            Binding percentBinding = new Binding();
            percentBinding.Source = dataBinding[filename + "percent"];
            BindingOperations.SetBinding(item.percent, TextBlock.TextProperty, percentBinding);

            taskList.Items.Add(item);
            TaskList.Li.Add(filename,new TaskInfo());
        }

        /// <summary>
        /// 尝试从本地读取任务列表并复制生成
        /// </summary>
        private void CreateList()
        {
            Dictionary<string,TaskInfo> copyList = TaskList.Li;
            Dictionary<string, long[]> progress = new Dictionary<string, long[]>();
            if(copyList!=null)
            {
                foreach (var i in copyList)
                {
                    NewTaskItem(i.Value.fileName);
                    tasks.Add(i.Value.fileName, new Download_Util(i.Value));
                    progress.Add(i.Value.fileName, new long[i.Value.totalRange]);
                    long fulled = (i.Value.current / i.Value.totalRange);//已完成的range数量
                    int l = 0;
                    if(fulled > 0)
                    {
                        for (int p = 0; p <= fulled - 1; p++)
                        {
                            progress[i.Value.fileName][p] = i.Value.rangeSize;
                            l = p;
                        }
                        progress[i.Value.fileName][l + 1] = (i.Value.current % i.Value.totalRange);
                    }
                    else
                        progress[i.Value.fileName][l] = (i.Value.current % i.Value.totalRange);
                }
                FileOperating.SetProgress(progress);
            }            
        }

        /// <summary>
        /// 后台数据结构添加任务
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="url"></param>
        public async Task taskAdd(string filename,string url)
        {
            tasks.Add(filename, new Download_Util());
            await tasks[filename].DownloadTask(url);
            NewTaskItem(filename);
            dataBinding[filename + "pauseContinue"] = "||";
        }

        /// <summary>
        /// 任务取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TaskCancel(object sender, RoutedEventArgs e)
        {
            Button template = (Button)e.Source;
            string name = template.Name;
            char[] taskname = new char[name.Length - 6];
            for(int i=0;i<=name.Length-7;i++)
            {
                taskname[i] = name[i];
            }
            string finalName = new string(taskname);
            tasks[finalName].Pause();
            tasks.Remove(finalName);
            TaskList.Li.Remove(finalName);
            dataBinding.Remove(finalName + "currentDown");
            dataBinding.Remove(finalName + "pauseContinue");
            dataBinding.Remove(finalName + "percent");
            dataBinding.Remove(finalName + "speed");
            taskList.Items.Remove(taskList.FindName(finalName));   
            FileOperating.DeleteFile(finalName);
        }

        public void TaskFinished(string filename)
        {
            tasks.Remove(filename);
            dataBinding.Remove(filename + "currentDown");
            dataBinding.Remove(filename + "pauseContinue");
            dataBinding.Remove(filename + "percent");
            dataBinding.Remove(filename + "speed");
            taskList.Items.Remove(taskList.FindName(filename));
            TaskList.Li.Remove(filename);
            TaskCount -= 1;
        }

        /// <summary>
        /// 任务暂停
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskPause(object sender, RoutedEventArgs e)
        {
            Button template = (Button)e.Source;
            string name = template.Name;
            char[] taskname = new char[name.Length - 10];
            for (int i = 0; i <= name.Length - 10 - 1; i++)
            {
                taskname[i] = name[i];
            }
            string finalName = new string(taskname);

            TaskList.Li[finalName] = tasks[finalName].Pause();//暂停任务时返回暂停的Task info
            dataBinding[name+"pauseContinue"] = ">";
        }

        public async Task TaskContinue(object sender,RoutedEventArgs e)
        {
            Button template = (Button)e.Source;
            string name = template.Name;
            char[] taskname = new char[name.Length - 10];
            for (int i = 0; i <= name.Length - 10 - 1; i++)
            {
                taskname[i] = name[i];
            }
            string finalName = new string(taskname);
            await tasks[finalName].ContinueTask();
            //未完成
            //-------------------------------------------------- 
            dataBinding[name+"pause_continue"] = "||";
        }

        /// <summary>
        /// 更新实际下载进度
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="progress"></param>
        public void UpdateProgress(string filename,long progress,long totalSize)
        {
            dtp.Dispatcher.Invoke(() =>
            {
                dataBinding[filename + "currentDone"] = Convert.ToDecimal(progress / 1024 / 1024).ToString("N2") + "/" + totalSize;
            });
            
        }

        /// <summary>
        /// 更新百分比显示
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="percent"></param>
        public void UpdatePercent(string filename,long percent)
        {
            dtp.Dispatcher.Invoke(() =>
            {

                dataBinding[filename + "percent"] = Convert.ToDecimal(percent * 100).ToString("N2") + "%";
            });
        }

        public void UpdateSpeed(string filename,long speed)
        {
            dtp.Dispatcher.Invoke(() =>
            {
                if(speed<1024*1024)
                {
                    dataBinding[filename + "speed"] = Convert.ToDecimal(speed / 1024).ToString("N2") + "kb/s";
                }
                else if(speed<1024&&speed>0)
                {
                    dataBinding[filename + "speed"] = "<1kb/s";
                }
                else if(speed==0)
                {
                    dataBinding[filename + "speed"] = "0kb/s";
                }
                else
                {
                    dataBinding[filename + "speed"] = Convert.ToDecimal(speed / 1024 / 1024).ToString("N2") + "mb/s";
                }
            });
        }

        public void Exit()
        {
            if(TaskCount!=0)
            {
                foreach(var i in tasks.Values)
                {
                    TaskList.Li[i.getFileName()] = i.Pause();
                }
                TaskList.SaveInfo();
            }
            else
            {
                TaskList.Li = null;
            }
        }

    }
}
