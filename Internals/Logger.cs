using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Text;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals
{
    public enum LogType {
        Info,
        Warn,
        Error,
        Debug
    }
    
    /// <summary>Represents a system which reads and writes to a logging file.</summary>
    public sealed class Logger : IDisposable {
        private StringBuilder stringBuilder = new(128);
        private readonly string writeTo;

        public readonly string Name;

        public readonly string FileName;

        private readonly Assembly assembly;

        private static FileStream fStream;
        private static StreamWriter sWriter;
        
        public Logger(string writeFile, string name) {
            assembly = Assembly.GetExecutingAssembly();
            Name = name;

            FileName = stringBuilder.Append(name).Append('_').Append(DateTime.Now.StringFormatCustom("_")).Append(".log").ToString();

            writeTo = Path.Combine(writeFile, $"{FileName}");

            Debug.WriteLine($"Created '{writeTo}'");

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
            var contentsAsString = contents.ToString();
            fStream.Position = fStream.Length;
            stringBuilder.Clear(); // Clear the sb to avoid writing stuff we don't really want.
            // Equivalent to $"[{DateTime.Now}] [{assembly.GetName().Name}] [{writeType}]: {contents}"
            stringBuilder
                .Append('[').Append(DateTime.Now.ToString()).Append("] ")
                .Append('[').Append(assembly.GetName().Name).Append("] ")
                .Append('[').Append(FromLogLevel(writeType)).Append("]: ")
                .Append(contentsAsString);

            var str = stringBuilder.ToString();
            sWriter.WriteLine(str);
            Debug.WriteLine(str);
            sWriter.Flush();

            if (throwException)
                throw new Exception(contentsAsString);
        }

        private static string FromLogLevel(LogType type) { // Converts a LogType to a string without the use of Reflection.
            return type switch {
                LogType.Info => "Info",
                LogType.Warn => "Warn",
                LogType.Error => "Error",
                LogType.Debug => "Debug",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        
        public void Dispose() {
            sWriter.Dispose();
            fStream.Dispose();
        }
        ~Logger() {
            Dispose();
        }
    }
}