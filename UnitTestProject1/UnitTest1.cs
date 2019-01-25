using System;
using System.Net.Http;
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
            Uri url = new Uri("https://osu.ppy.sh/beatmapsets/886566/download");
            Downloader.Download_Util download = new Downloader.Download_Util();
            long fileSize = 0;
            Downloader.Conf c = new Downloader.Conf();
            string filename = "test.orz";
            c.buffer = 64 * 1024 * 1024;
            c.infoPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            c.maxThread = 4;
            c.storagePath = "D:\\doltest";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            fileSize = response.Content.Headers.ContentLength.Value;
            Console.WriteLine(fileSize);
            Downloader.FileOperating_Util.path = c.storagePath;
            download.cutPosition = new long[1];
            download.cutPosition[0] = fileSize;
            Downloader.FileOperating_Util.CreateFile(filename, fileSize, 1);
            await download.FileDownloadAsync(url, 0, fileSize,0);
        }
    }
}
