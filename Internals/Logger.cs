using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Globalization;
using System.Text;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals;

public enum LogType {
    /// <summary>Information about what is happening during runtime to serve as a sequence of events to
    /// any kind of fatal error.</summary>
    Info,
    /// <summary>A logged warning that should serve as a heads-up to a developer or player that something
    /// has not gone as expected in runtime.</summary>
    Warn,
    /// <summary>For an error that should attempt to end the runtime, as the problem caused would make the process
    /// not continue to function as expected.</summary>
    ErrorFatal,
    /// <summary>For an error that would normally be fatal, but has been supressed to allow the process to continue
    /// using defensive programming techniques.</summary>
    ErrorSilent,
    /// <summary>For when the logger is writing debug information.</summary>
    Debug
}

/// <summary>Represents a system which reads and writes to a logging file.</summary>
public sealed class Logger : IDisposable {
    private readonly StringBuilder _stringBuilder = new(128);
    private readonly string writeTo;

    public readonly string Name;

    public readonly string FileName;

    private readonly Assembly assembly;

    private static FileStream fStream;
    private static StreamWriter sWriter;
    
    public Logger(string writeFile, string name) {
        assembly = Assembly.GetExecutingAssembly();
        Name = name;

        FileName = _stringBuilder.Append(name).Append('_').Append(DateTime.Now.StringFormatCustom("_")).Append(".log").ToString();

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
        lock (sWriter) {
            _stringBuilder.Clear(); // Clear the sb to avoid writing stuff we don't really want.
            // Equivalent to $"[{DateTime.Now}] [{assembly.GetName().Name}] [{writeType}]: {contents}"
            _stringBuilder
                .Append('[').Append(DateTime.Now.ToString(CultureInfo.InvariantCulture)).Append("] ")
                .Append('[').Append(assembly.GetName().Name).Append("] ")
                .Append('[').Append(FromLogLevel(writeType)).Append("]: ")
                .Append(contentsAsString);

            var str = _stringBuilder.ToString();
            sWriter.WriteLine(str);
            Debug.WriteLine(str);
            if (GameLauncher.IsConsoleAllocated) Console.WriteLine(str);
            sWriter.Flush();
        }

        if (throwException)
            throw new Exception(contentsAsString);
    }

    private static string FromLogLevel(LogType type) { // Converts a LogType to a string without the use of Reflection.
        return type switch {
            LogType.Info => "Info",
            LogType.Warn => "Warn",
            LogType.ErrorFatal => "ErrorFatal",
            LogType.ErrorSilent => "ErrorSilent",
            LogType.Debug => "Debug",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    
    public void Dispose() {
        sWriter.Dispose();
        fStream.Dispose();
        GC.SuppressFinalize(this);
    }
    ~Logger() {
        Dispose();
    }
}