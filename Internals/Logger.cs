using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;

namespace WiiPlayTanksRemake.Internals
{
    public sealed class Logger : IDisposable
    {
        private readonly string writeTo;
        public string Name
        {
            get;
        }

        private readonly Assembly assembly;

        private static FileStream fStream;
        private static StreamWriter sWriter;

        public enum LogType
        {
            Info,
            Warn,
            Error,
            Debug
        }

        public Logger(string writeFile, string name) {
            assembly = Assembly.GetExecutingAssembly();
            Name = name;
            writeTo = writeFile;

            string withName = Path.Combine(writeFile, $"{name}.log");

            fStream = new(withName, FileMode.OpenOrCreate);
            sWriter = new(fStream);
        }
        public void Write(object write, LogType writeType) {
            string str = $"[{assembly.GetName().Name}] [{writeType}]: {write}";
            sWriter.WriteLine(str);
            Debug.WriteLine(str);
        }

        public void Dispose() {
            sWriter?.Dispose();
            fStream?.Dispose();
        }
    }
}