using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderWinForm
{
    class Utils
    {
        public static readonly UTF8Encoding UTF8WithoutBOM = new UTF8Encoding(false);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileExW([In] string lpExistingFileName, [In] string lpNewFileName, [In] int dwFlags);
        const int MOVEFILE_REPLACE_EXISTING = 0x1;

        /// <summary>
        /// Save text to a file atomically.
        /// </summary>
        public static void SaveTextAtomic(string path, string content)
        {
            var tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, content, UTF8WithoutBOM);
            if (!MoveFileExW(tmpPath, path, MOVEFILE_REPLACE_EXISTING))
            {
                throw new Exception("Error saving file " + path, new Win32Exception());
            }
        }

        public static string GetText(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
