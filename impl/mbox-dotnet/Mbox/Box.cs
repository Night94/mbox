using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mbox;

[AttributeUsage(AttributeTargets.Class)]
public sealed class BoxImplementationAttribute : Attribute
{
    public BoxImplementationAttribute(string unit) => Unit = unit;
    public string Unit { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationHandlerAttribute : Attribute
{
    public OperationHandlerAttribute(string interfaceUnit, string operation)
    {
        InterfaceUnit = interfaceUnit;
        Operation = operation;
    }

    public string InterfaceUnit { get; }
    public string Operation { get; }
}

public abstract class Box
{
    public IBoxContext Context { get; internal set; } = null!;

    public virtual Task InitAsync() => Task.CompletedTask;
    public virtual Task RunAsync() => Task.CompletedTask;
    public virtual Task DeinitAsync() => Task.CompletedTask;
}

public interface IBoxContext
{
    string SelfAddress { get; }
    bool IsCancelled { get; }

    Task<ResponsePayload> RequestAsync(
        string interfaceUnit,
        string operation,
        object? payload,
        int? targetInstanceId = null,
        int? timeoutMs = null);

    Task SendOnceAsync(
        string interfaceUnit,
        string operation,
        object? payload,
        int? targetInstanceId = null);

    Task<int> CreateBoxAsync(string unit);
    Task DestroyAsync(string address);
    JsonNode? GetConfigItem(string key);
    T? GetConfigItem<T>(string key);
    void Shutdown();
    void Log(LogCategory category, string content);
}

public enum ResponseStatus
{
    Ok,
    Error,
    Exception
}

public sealed class ResponsePayload
{
    public required ResponseStatus Status { get; init; }
    public string? Text { get; init; }
    public JsonNode? Result { get; init; }

    public T? ResultAs<T>() =>
        Result is null ? default : Result.Deserialize<T>(Framework.JsonOptions);

    public ResponsePayload ThrowIfException()
    {
        if (Status == ResponseStatus.Exception)
            throw new RemoteBoxException(Text ?? "Remote operation failed.");
        return this;
    }
}

public sealed class OperationError : Exception
{
    public OperationError(string failure) : base(failure) => Failure = failure;
    public string Failure { get; }
}

public sealed class RemoteBoxException : Exception
{
    public RemoteBoxException(string text) : base(text) { }
}
