using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;

namespace WiiPlayTanksRemake.Internals
{
    public enum LogType
    {
        Info,
        Warn,
        Error,
        Debug
    }

    /// <summary>Represents a system which reads and writes to a logging file.</summary>
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
        
        public Logger(string writeFile, string name) {
            assembly = Assembly.GetExecutingAssembly();
            Name = name;

            writeTo = Path.Combine(writeFile, $"{name}.log");

            fStream = new(writeTo, FileMode.OpenOrCreate);
            fStream.SetLength(0);
            sWriter = new(fStream);
        }

        /// <summary>
        /// Writes content to a logging file.
        /// </summary>
        /// <param name="contents">The content to write.</param>
        /// <param name="writeType">The type of logging being done.</param>
        /// <param name="throwException">Whether or not to throw an exception upon write completion.</param>
        /// <exception cref="Exception">If <paramref name="throwException"/> is set to <see langword="true"/>, this exception will be thrown upon write completion.</exception>
        public void Write(object contents, LogType writeType, bool throwException = false) {
            fStream.Position = fStream.Length;
            string str = $"[{DateTime.Now}] [{assembly.GetName().Name}] [{writeType}]: {contents}";
            sWriter.WriteLine(str);
            Debug.WriteLine(str);
            sWriter.Flush();

            if (throwException)
                throw new Exception(contents.ToString());
        }

        public void Dispose() {
            sWriter?.Dispose();
            fStream?.Dispose();
        }

        ~Logger()
        {
            Dispose();
        }
    }
}