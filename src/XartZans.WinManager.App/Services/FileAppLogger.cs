using System.Globalization;
using System.IO;

namespace XartZans.WinManager.App.Services;

public sealed class FileAppLogger : IAppLogger
{
    private readonly object _sync = new();
    private readonly string _logFilePath;

    public FileAppLogger()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XartZans",
            "WinManager",
            "Logs");
        _logFilePath = Path.Combine(LogDirectory, $"winmanager-{DateTime.Now:yyyyMMdd}.log");
    }

    public string LogDirectory { get; }

    public void Information(string message) => Write("INFO", message);

    public void Error(string message, Exception exception) =>
        Write("ERROR", $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message)
    {
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    _logFilePath,
                    $"{DateTimeOffset.Now.ToString("u", CultureInfo.InvariantCulture)} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never cause an application failure.
        }
    }
}
