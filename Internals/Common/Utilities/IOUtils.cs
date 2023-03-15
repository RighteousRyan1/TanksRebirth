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
    public static byte[] ToAsciiBytes(this string input) {
        return Encoding.ASCII.GetBytes(input);
    }
    public static string[] GetSubFolders(string root, bool getName = false) {
        var rootGotten = Directory.GetDirectories(root);

        var names = new string[rootGotten.Length];

        if (!getName) 
            return rootGotten;

        var curTotal = 0;
        foreach (var dir in rootGotten) {
            var dirInfo = new DirectoryInfo(dir);

            names[curTotal] = dirInfo.Name;
            curTotal++;
        }
        return names; 
    }
    /// <remarks>This method is recursive.</remarks>
    public static void CopyFolder(string sourceFolder, string destFolder, bool deleteOld) {
        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        var files = Directory.GetFiles(sourceFolder);
        var folders = Directory.GetDirectories(sourceFolder);

        // The disk letter is different, we can not move files, even if we wanted!
        var isDifferentDrive = sourceFolder[0] != destFolder[0];

        // First get to the deepest folder level. Then copy the files to destination. Gotta love recursion.
        
        foreach (var folder in folders) {
            var name = Path.GetFileName(folder);
            var dest = Path.Combine(destFolder, name);
            CopyFolder(folder, dest, deleteOld);
        }

        foreach (var file in files) {
            var name = Path.GetFileName(file);
            var dest = Path.Combine(destFolder, name);
            
            if (!isDifferentDrive && deleteOld) { 
                // If we are requested to delete the old and we are on the same drive, just move it, its cheaper.
                File.Move(file, dest, true);
            }
            File.Copy(file, dest, true);
            if (deleteOld)
                File.Delete(file);
        }

    }
}
