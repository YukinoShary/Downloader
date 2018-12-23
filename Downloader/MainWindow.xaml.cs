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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow mw;
        public MainWindow()
        {
            InitializeComponent();
            mw = this;
            Conf.getConf();
            FileOperating_Util.path = Conf.config.storagePath;
            TaskInfo.getTaskInfo();            
            DownloadTasksPage.dtp = new DownloadTasksPage();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ControlTemplate template = mainwindow.FindName("controlTemplate") as ControlTemplate;
            if(template!=null)
            {
                Frame f = template.FindName("main",mainwindow) as Frame;
                f.Navigate(new settingPage());
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ControlTemplate template = mainwindow.FindName("controlTemplate") as ControlTemplate;
            if (template != null)
            {
                Frame f = template.FindName("main", mainwindow) as Frame;
                f.Navigate(DownloadTasksPage.dtp);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Wait w = new Wait();
            w.Show();
            TaskInfo.Li.Clear();
            DownloadTasksPage.dtp.SaveList();
            Application.Current.MainWindow.Close();
        }
    }
}
