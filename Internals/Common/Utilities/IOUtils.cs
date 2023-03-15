using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class IOUtils
{
    public static byte[] ToAsciiBytes(string input)
    {
        List<byte> vs = new();
        for (int i = 0; i < input.Length; i++)
            vs.Add(Convert.ToByte(input[i]));
        return vs.ToArray();
    }
    public static string[] GetSubFolders(string root, bool getName = false)
    {
        var rootGotten = Directory.GetDirectories(root);

        string[] names = new string[rootGotten.Length];

        int curTotal = 0;
        if (getName)
        {
            foreach (var dir in rootGotten)
            {
                var dirInfo = new DirectoryInfo(dir);

                names[curTotal] = dirInfo.Name;
                curTotal++;
            }
        }
        return !getName ? rootGotten : names;
    }
    public static void CopyFolder(string sourceFolder, string destFolder, bool deleteOld)
    {
        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        string[] files = Directory.GetFiles(sourceFolder);
        string[] folders = Directory.GetDirectories(sourceFolder);

        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            string dest = Path.Combine(destFolder, name);
            File.Copy(file, dest, true);
            if (deleteOld)
                File.Delete(file);
        }

        foreach (string folder in folders)
        {
            string name = Path.GetFileName(folder);
            string dest = Path.Combine(destFolder, name);
            CopyFolder(folder, dest, deleteOld);
        }
    }
}
