using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class WebUtils
    {
        public static byte[] DownloadWebFile(string url, out string fileName)
        {
            var data = m_DownloadWebFile(url).GetAwaiter().GetResult();

            fileName = data.Item2;
            return data.Item1;
        }
        private static async Task<(byte[], string)> m_DownloadWebFile(string url)
        {
            using var client = new HttpClient();

            var file = await client.GetByteArrayAsync(url);

            GameHandler.ClientLog.Write($"WebRequest sent to '{url}'", LogType.Debug);

            var name = System.IO.Path.GetFileName(url).Split('?')[0];

            return (file, name);
        }
    }
}
