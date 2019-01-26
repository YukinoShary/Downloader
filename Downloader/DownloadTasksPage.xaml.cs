using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private Dictionary<string, FileDownloader> tasks = new Dictionary<string, FileDownloader>();

        public DownloadTasksPage()
        {
            InitializeComponent();
            dtp = this;
            dataBinding = new Dictionary<string, string>();
            CreateList();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NewTask nt = new NewTask() { Owner = Window.GetWindow(this) };
            nt.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            nt.Show();
        }

        /// <summary>
        /// 前台显示添加任务项
        /// </summary>
        /// <param name="filename"></param>
        private void NewTaskItem(string filename)
        {
            TaskCount += 1;
            Grid g = new Grid();
            g.Height = 70;
            g.Width = 600;
            g.Name = filename;
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            g.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            g.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            g.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            //文件名显示
            TextBlock name = new TextBlock();
            name.HorizontalAlignment = HorizontalAlignment.Left;
            name.Text = filename;
            name.Margin = new Thickness(10, 0, 0, 0);
            //实际下载/总大小          
            TextBlock currentDone = new TextBlock();
            currentDone.Name = filename + "currentDown";
            currentDone.HorizontalAlignment = HorizontalAlignment.Left;
            currentDone.Margin = new Thickness(10, 10, 0, 0);
            dataBinding.Add(filename + "currentDown", "");
            Binding processBinding = new Binding();
            processBinding.Source = dataBinding[filename + "currentDown"];
            BindingOperations.SetBinding(currentDone, TextBlock.TextProperty, processBinding);//数据绑定
            //取消任务           
            Button cancelTask = new Button();
            cancelTask.Name = filename + "cancel";
            cancelTask.Content = "X";
            cancelTask.Width = 20;
            cancelTask.Height = 20;
            cancelTask.HorizontalAlignment = HorizontalAlignment.Right;
            cancelTask.Click += TaskCancel;
            //暂停/继续按钮           
            Button pause_continue = new Button();
            pause_continue.Content = ">";
            pause_continue.Name = filename + "pause_continue";
            pause_continue.Width = 50;
            pause_continue.Height = 20;
            pause_continue.Margin = new Thickness(0, 0, 20, 0);
            pause_continue.HorizontalAlignment = HorizontalAlignment.Right;
            pause_continue.Click += TaskPause;           
            dataBinding.Add(filename + "pause_continue", ">");
            Binding buttonBinding = new Binding();
            buttonBinding.Source = dataBinding[filename + "pause_continue"];
            BindingOperations.SetBinding(pause_continue, Button.ContentProperty, buttonBinding);
            //百分比显示
            TextBlock percent = new TextBlock();
            percent.Name = filename + "percent";
            percent.HorizontalAlignment = HorizontalAlignment.Right;
            percent.Margin = new Thickness(0, 10, 10, 0);
            dataBinding.Add(filename + "percent", "");
            Binding percentBinding = new Binding();
            percentBinding.Source = dataBinding[filename + "percent"];
            BindingOperations.SetBinding(percent, TextBlock.TextProperty, percentBinding);

            TextBlock speed = new TextBlock();
            speed.Name = filename + "speed";
            speed.Margin = new Thickness(0, 10, 0, 0);
            dataBinding.Add(filename + "speed", "");
            Binding speedBinding = new Binding();
            speedBinding.Source = dataBinding[filename + "speed"];
            BindingOperations.SetBinding(speed, TextBlock.TextProperty, speedBinding);

            g.Children.Add(pause_continue);
            g.Children.Add(cancelTask);
            g.Children.Add(currentDone);
            g.Children.Add(name);
            g.Children.Add(percent);
            g.Children.Add(speed);

            Grid.SetColumn(cancelTask, 2);
            Grid.SetRow(cancelTask, 0);
            Grid.SetRow(currentDone, 1);
            Grid.SetColumn(currentDone, 0);
            Grid.SetRow(pause_continue, 0);
            Grid.SetColumn(pause_continue, 2);
            Grid.SetRow(name, 0);
            Grid.SetColumn(name, 0);
            Grid.SetRow(percent, 1);
            Grid.SetColumn(percent, 2);
            Grid.SetColumn(speed, 1);
            Grid.SetRow(speed, 1);

            taskList.Items.Add(g);
        }

        /// <summary>
        /// 尝试从本地读取任务列表并复制生成
        /// </summary>
        private void CreateList()
        {
            List<TaskInfo> copyList = TaskInfo.Li;
            if(copyList!=null)
            {
                for (int i = 0; i <= copyList.Count-1; i++)
                {
                    // TODO
                    //NewTaskItem(copyList[i].fileName);
                    //tasks.Add(copyList[i].fileName, new Download_Util(copyList[i]));
                }
            }            
        }

        /// <summary>
        /// 后台数据结构添加任务
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="url"></param>
        public void TaskAdd(string filename,string url)
        {
            var path = System.IO.Path.Combine(Conf.config.storagePath, filename);
            tasks.Add(filename, new FileDownloader() { SourceUri = new Uri(url), DestFilePath = filename });
            tasks[filename].Start();
            NewTaskItem(filename);
            dataBinding[filename + "pause_continue"] = "||";
        }

        /// <summary>
        /// 任务取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskCancel(object sender, RoutedEventArgs e)
        {
            Button template = (Button)e.Source;
            string name = template.Name;
            string finalName = name.Substring(0, name.Length - 6);
            // TODO
            //tasks[finalName].PauseTask();
            tasks.Remove(finalName);
            dataBinding.Remove(finalName + "currentDown");
            dataBinding.Remove(finalName + "pause_continue");
            dataBinding.Remove(finalName + "percent");
            dataBinding.Remove(finalName + "speed");
            taskList.Items.Remove(taskList.FindName(finalName));
            // TODO
            //FileOperating_Util.DeleteFile(finalName);
        }

        public void TaskFinished(string filename)
        {
            tasks.Remove(filename);
            dataBinding.Remove(filename + "currentDown");
            dataBinding.Remove(filename + "pause_continue");
            dataBinding.Remove(filename + "percent");
            dataBinding.Remove(filename + "speed");
            taskList.Items.Remove(taskList.FindName(filename));
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
            // TODO
            //tasks[finalName].PauseTask();
            //saveinfo
            dataBinding[name+"pause_continue"] = ">";
        }

        private void TaskContinue(object sender,RoutedEventArgs e)
        {
            Button template = (Button)e.Source;
            string name = template.Name;
            char[] taskname = new char[name.Length - 10];
            for (int i = 0; i <= name.Length - 10 - 1; i++)
            {
                taskname[i] = name[i];
            }
            string finalName = new string(taskname);
            // TODO
            //tasks[finalName].ContinueTask();
            //saveinfo
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
                dataBinding[filename + "currentDone"] = (progress / 1024 / 1024).ToString("N0") + "/" + totalSize;
            });
            
        }

        /// <summary>
        /// 更新百分比显示
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="percent"></param>
        public void UpdatePercent(string filename, float percent)
        {
            dtp.Dispatcher.Invoke(() =>
            {

                dataBinding[filename + "percent"] = (percent * 100).ToString("N2") + "%";
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

        public void SaveList()
        {
            // TODO
            //if (TaskCount!=0)
            //{
            //    foreach (var i in tasks.Values)
            //    {
            //        TaskInfo.Li.Add(i.SaveInfomation());
            //    }
            //    FileOperating_Util.SaveInfo(TaskInfo.InfoSerialize(), Conf.config.infoPath);
            //}
            //else
            //{
                TaskInfo.Li = null;
            //}
        }

    }
}
