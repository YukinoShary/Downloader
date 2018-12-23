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
using System.Windows.Forms;

namespace Downloader
{
    /// <summary>
    /// settingPage.xaml 的交互逻辑
    /// </summary>
    public partial class settingPage : Page
    {
        public settingPage()
        {
            this.InitializeComponent();

            //下拉框初始化
            Buffer.Items.Add(new KeyValuePair<string, long>("64 MB", 64*1024*1024));
            Buffer.Items.Add(new KeyValuePair<string, long>("128 MB", 128*1024*1024));
            Buffer.Items.Add(new KeyValuePair<string, long>("256 MB", 256*1024*1024));
            Buffer.SelectedValue = Conf.config.buffer;

            Thread.Items.Add(new KeyValuePair<string, int>("2线程", 2));
            Thread.Items.Add(new KeyValuePair<string, int>("4线程", 4));
            Thread.Items.Add(new KeyValuePair<string, int>("8线程", 8));
            Thread.SelectedValue = Conf.config.maxThread;

            //值填充

            downloadpath.Text = Conf.config.storagePath;
            infopath.Text = Conf.config.infoPath;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Conf.config.buffer = (long)Buffer.SelectedValue;
            Conf.config.maxThread = (int)Thread.SelectedValue;
            Conf.config.infoPath = infopath.Text + "info.inf";
            Conf.config.storagePath = downloadpath.Text + "stoarage.stg";
            Conf.SaveConf();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if(result==DialogResult.OK)
            {
                downloadpath.Text = dialog.SelectedPath;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                infopath.Text = dialog.SelectedPath;
            }
        }
    }
}
