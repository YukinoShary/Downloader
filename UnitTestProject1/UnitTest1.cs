using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Downloader;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Task.Run(async () =>
            {
                Uri url = new Uri("https://osu.ppy.sh/beatmapsets/886566/download");
                FileDownloader download = new FileDownloader() { SourceUri = url, DestFilePath = "test.orz" };
                await download.Start();
            }).Wait();
        }
    }
}
