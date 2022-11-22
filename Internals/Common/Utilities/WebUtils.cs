using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Net;

namespace TanksRebirth.Internals.Common.Utilities;

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

        // GameHandler.ClientLog.Write($"WebRequest sent to '{url}'", LogType.Debug); make a logger.. of course.

        var name = System.IO.Path.GetFileName(url).Split('?')[0];

        return (file, name);
    }

    public static bool RemoteFileExists(string url)
    {
        try
        {
            // i copied this code lmfao.

            //Creating the HttpWebRequest
            var request = WebRequest.Create(url) as HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            //Getting the Web Response.
            var response = request.GetResponse() as HttpWebResponse;
            //Returns TRUE if the Status code == 200
            var code = response.StatusCode;
            response.Close();
            return (code == HttpStatusCode.OK);
        }
        catch
        {
            return false;
        }
    }
}
