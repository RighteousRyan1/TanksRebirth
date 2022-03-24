using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
{
    public static class IOUtils
    {
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
    }
}
