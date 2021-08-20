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

            writeTo = Path.Combine(writeFile, $"{name}.log");

            fStream = new(writeTo, FileMode.OpenOrCreate);
            fStream.SetLength(0);
            sWriter = new(fStream);
        }

        public void Write(object contents, LogType writeType) {
            fStream.Position = fStream.Length;
            string str = $"[{DateTime.Now}] [{assembly.GetName().Name}] [{writeType}]: {contents}";
            sWriter.WriteLine(str);
            Debug.WriteLine(str);
            sWriter.Flush();
        }

        public void Dispose() {
            sWriter?.Dispose();
            fStream?.Dispose();
        }
    }
}