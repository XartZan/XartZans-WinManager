namespace XartZans.WinManager.App.Services;

public interface IAppLogger
{
    string LogDirectory { get; }
    void Information(string message);
    void Error(string message, Exception exception);
}
