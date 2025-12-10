using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Globalization;
using System.Text;
using TanksRebirth.Internals.Common.Utilities;
using System.Diagnostics.CodeAnalysis;

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

public delegate void OnLog(Assembly assembly, string data, LogType logType);

/// <summary>Represents a system which reads and writes to a logging file.</summary>
public sealed class Logger : IDisposable {
    readonly StringBuilder _builder = new(128);
    readonly string _filePath;
    readonly FileStream _stream;
    readonly StreamWriter _writer;

    public event OnLog? OnLogWrite;

    public string Name { get; }
    public string FileName { get; }

    public Logger(string writeFile, string name) {
        Name = name;

        FileName = _builder.Append(name).Append('_').Append(DateTime.Now.StringFormatCustom("_")).Append(".log").ToString();

        _filePath = Path.Combine(writeFile, $"{FileName}");

        Debug.WriteLine($"Created '{_filePath}'");

        _stream = new(_filePath, FileMode.OpenOrCreate);
        _stream.SetLength(0);
        _writer = new(_stream);
    }

    /// <summary>
    /// Writes content to a logging file.
    /// </summary>
    /// <param name="contents">The content to write.</param>
    /// <param name="writeType">The type of logging being done.</param>
    /// <param name="throwException">Whether or not to throw an exception upon write completion.</param>
    /// <exception cref="Exception">If <paramref name="throwException"/> is set to <see langword="true"/>, this exception will be thrown upon write completion.</exception>
    public void Write(object contents, LogType writeType, bool throwException = false) {
        var contentsAsString = contents.ToString()!;
        _stream.Position = _stream.Length;
        lock (_writer) {
            _builder.Clear(); // Clear the sb to avoid writing stuff we don't really want.
            // Equivalent to $"[{DateTime.Now}] [{assembly.GetName().Name}] [{writeType}]: {contents}"

            var callingAssembly = Assembly.GetCallingAssembly();
            _builder
                .Append('[').Append(DateTime.Now.ToString(CultureInfo.InvariantCulture)).Append("] ")
                .Append('[').Append(callingAssembly.GetName().Name).Append("] ")
                .Append('[').Append(FromLogLevel(writeType)).Append("]: ")
                .Append(contentsAsString);

            var finalStr = _builder.ToString();
            _writer.WriteLine(finalStr);
            Debug.WriteLine(finalStr);
            OnLogWrite?.Invoke(callingAssembly, contentsAsString, writeType);
            if (GameLauncher.IsConsoleAllocated) Console.WriteLine(finalStr);
            _writer.Flush();
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
        _writer.Dispose();
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
    ~Logger() {
        Dispose();
    }
}