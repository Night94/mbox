namespace Mbox;

public enum LogCategory
{
    Error,
    Warning,
    Normal,
    Debug
}

public sealed class Logger
{
    private readonly string _appName;
    private readonly object _lock = new();

    internal Logger(string appName) => _appName = appName;

    public void Emit(LogCategory category, string boxName, int instanceId, string content)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var line = $"{timestamp} [{category,-7}] {_appName} {boxName}|{instanceId}  {content}";
        lock (_lock)
            Console.Out.WriteLine(line);
    }
}
