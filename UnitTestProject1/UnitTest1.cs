using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            string url = "https://osu.ppy.sh/beatmapsets/886566/download";
            Downloader.Download_Util download = new Downloader.Download_Util();
            Downloader.Conf c = new Downloader.Conf();
            c.buffer = 64 * 1024 * 1024;
            c.infoPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            c.maxThread = 4;
            c.storagePath = "D:\\doltest";
            Downloader.FileOperating_Util.path = c.storagePath;
            await download.DownloadTask(url, "testdownload");  
        }
    }
}
