using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader
{
    public class FileDownloader
    {
        // Settings:
        public string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36";
        public int MaxThread = 5;
        public int MinRangeSize = 1 * 1024 * 1024; // 1 MiB
        public Uri SourceUri;
        public string DestFilePath;
        public HttpClient HttpClient = new HttpClient();

        // Status:
        public Worker MainWorker;
        public DownloadState State;
        public List<Range> Ranges = new List<Range>();
        public bool RangeSupported;
        public long Length = -2; // -2: requesting, -1: unknown (chunked encoding?)
        public long Downloaded;

        public FileStream DestFile;

        public FileDownloader()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public async Task Start()
        {
            State = DownloadState.Requesting;
            try
            {
                MainWorker = new Worker(this);
                var resp = await MainWorker.Request(new HttpRequestMessage(HttpMethod.Get, SourceUri));
                resp.EnsureSuccessStatusCode();
                RangeSupported = resp.Headers.AcceptRanges.Contains("bytes");
                Length = GetLength(resp);
                State = DownloadState.InitFile;
                InitFile();
                InitRanges();
                for (int i = 0; i < Ranges.Count; i++)
                {
                    var range = Ranges[i];
                    if (i == 0)
                    {
                        // keep using the first reponse to download the first range
                        range.Worker = MainWorker;
                        range.WorkerTask = range.Worker.DownloadRange(range, resp);
                    }
                    else
                    {
                        range.Worker = new Worker(this);
                        range.WorkerTask = range.Worker.DownloadRange(range);
                    }
                }
                State = DownloadState.Downloading;
                await Task.WhenAll(Ranges.Select(x => x.WorkerTask));
                State = DownloadState.Success;
            }
            catch (Exception e)
            {
                State = DownloadState.Error;
                throw;
            }
            finally
            {
                if (DestFile != null)
                {
                    DestFile.Dispose();
                    DestFile = null;
                }
            }
        }

        private void InitFile()
        {
            DestFile = File.Open(DestFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            if (Length > 0)
                DestFile.SetLength(Length);
        }

        private void InitRanges()
        {
            if (RangeSupported && Length >= MinRangeSize * 2)
            {
                var rangeCount = Math.Min(Length / MinRangeSize, MaxThread);
                var lenPerRange = Length / rangeCount;
                long cur = 0;
                for (int i = 0; i < rangeCount - 1; i++)
                {
                    Ranges.Add(new Range { Offset = cur, Length = lenPerRange });
                    cur += lenPerRange;
                }
                Ranges.Add(new Range { Offset = cur, Length = Length - cur });
            }
            else
            {
                Ranges.Add(new Range { Offset = 0, Length = Length });
            }
        }

        private static long GetLength(HttpResponseMessage resp)
        {
            if (resp.Headers.TransferEncodingChunked == true)
                return -1;
            var lenStr = resp.Content.Headers.GetValues("Content-Length").SingleOrDefault();
            if (lenStr == null)
                return -1;
            return long.Parse(lenStr);
        }

        public class Range
        {
            public long Offset;
            public long Length;
            public long Current;
            public long CurrentWithOffset => Offset + Current;
            public long Remaining => Length - Current;
            public Task WorkerTask;
            public CancellationTokenSource CancellationToken;
            public Worker Worker;
        }

        public class Worker
        {
            public FileDownloader FileDownloader;
            public DownloadState State;
            public CancellationToken CancellationToken;
            HttpClient HttpClient => FileDownloader.HttpClient;

            public Worker(FileDownloader fileDownloader)
            {
                FileDownloader = fileDownloader;
            }

            public async Task<HttpResponseMessage> Request(HttpRequestMessage httpRequest)
            {
                State = DownloadState.Requesting;
                try
                {
                    return await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, CancellationToken);
                }
                catch (Exception)
                {
                    State = DownloadState.Error;
                    throw;
                }
            }

            public async Task DownloadRange(FileDownloader.Range range)
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, FileDownloader.SourceUri);
                    req.Headers.Range = new RangeHeaderValue(range.CurrentWithOffset, range.Offset + range.Length - 1);
                    var resp = await Request(req);
                    CancellationToken.ThrowIfCancellationRequested();
                    resp.EnsureSuccessStatusCode();
                    await DownloadRange(range, resp);
                }
                catch (Exception e)
                {
                    State = DownloadState.Error;
                }
            }

            public async Task DownloadRange(FileDownloader.Range range, HttpResponseMessage resp)
            {
                try
                {
                    bool unknownLength = range.Length == -1;
                    using (var stream = await resp.Content.ReadAsStreamAsync())
                    {
                        var fs = FileDownloader.DestFile;
                        const int MaxBufLen = 64 * 1024;
                        int bufLen = unknownLength ? MaxBufLen : (int)Math.Min(range.Remaining, MaxBufLen);
                        var buf = new byte[bufLen];
                        while (range.Remaining > 0 || unknownLength)
                        {
                            int readLen = unknownLength ? bufLen : (int)Math.Min(range.Remaining, bufLen);
                            readLen = await stream.ReadAsync(buf, 0, readLen);
                            if (readLen == 0)
                            {
                                if (unknownLength)
                                    break;
                                else
                                    throw new Exception("Unexpected end-of-stream");
                            }
                            lock (fs) // use multiple FileStream instances to avoid locking (?)
                            {
                                if (fs.Position != range.CurrentWithOffset)
                                    fs.Position = range.CurrentWithOffset;
                                fs.Write(buf, 0, readLen); // no need to use async IO on file (?)
                            }
                            range.Current += readLen; // update Current **after** write to the file
                        }
                    }
                    State = DownloadState.Success;
                }
                catch (Exception)
                {
                    State = DownloadState.Error;
                    throw;
                }
            }
        }
    }

    public enum DownloadState
    {
        None,
        Requesting,
        InitFile,
        Downloading,
        Success,
        Error
    }
}
