using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Mbox;

internal enum BoxState
{
    Init,
    Running,
    Deinit,
    Error
}

internal enum MessageMethod
{
    Req,
    Once
}

internal sealed record RoutedMessage(
    string Receiver,
    string Sender,
    MessageMethod Method,
    string InterfaceUnit,
    string Operation,
    string Id,
    JsonNode? Payload);

internal sealed record HandlerMethod(MethodInfo Method, Type PayloadType, Type? ResultType);

public sealed class Framework
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Composition _composition;
    private readonly Dictionary<string, Type> _implementations;
    private readonly ConcurrentDictionary<string, BoxInstance> _instances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _nextInstanceId = new(StringComparer.Ordinal);
    private readonly List<BoxInstance> _frameworkOwned = [];
    private readonly object _gate = new();
    private readonly TaskCompletionSource _shutdownCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _shutdownRequested;

    private Framework(Composition composition, IEnumerable<Assembly> implementationAssemblies)
    {
        _composition = composition;
        _implementations = DiscoverImplementations(implementationAssemblies);
        foreach (var box in composition.Boxes.Keys)
        {
            if (!_implementations.ContainsKey(box))
                throw new InvalidOperationException($"No [BoxImplementation(\"{box}\")] type was found.");
        }
        ValidateRuntimeConfiguration();
        DefaultMessageTimeoutMs = GetRuntimeValue("runtime.defaultMsgTimeoutMs", 10000);
        DestroyTimeoutMs = GetRuntimeValue("runtime.destroyTimeoutMs", 5000);
        LogLateResponses = GetRuntimeValue("runtime.logLateResponses", false);
        SendRemoteExceptionStacks = GetRuntimeValue("runtime.sendRemoteExceptionStacks", true);
        Log = new Logger(composition.AppName);
    }

    public string ApplicationName => _composition.AppName;
    public int DefaultMessageTimeoutMs { get; }
    public int DestroyTimeoutMs { get; }
    public bool LogLateResponses { get; }
    public bool SendRemoteExceptionStacks { get; }
    public Logger Log { get; }

    public static Framework Load(string appUnitPath, string configPath, params Assembly[] implementationAssemblies)
    {
        var applicationConfig = File.Exists(configPath)
            ? JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject
                ?? throw new InvalidOperationException($"Configuration '{configPath}' is not a JSON object.")
            : new JsonObject();
        var composition = Composition.Load(appUnitPath, applicationConfig);
        return new Framework(composition, implementationAssemblies);
    }

    public async Task RunAsync()
    {
        var entry = CreateInstance(_composition.EntryBox, owner: null, frameworkOwned: true, defaultInstance: true);
        await entry.StartAsync();
        entry.DispatchRun();
        await _shutdownCompletion.Task;
    }

    internal OperationContract GetOperation(string interfaceUnit, string operation) =>
        _composition.GetOperation(interfaceUnit, operation);

    internal async Task<BoxInstance> ResolveTargetAsync(
        BoxInstance caller,
        string interfaceUnit,
        string operation,
        int? targetInstanceId)
    {
        var provider = _composition.ResolveProvider(caller.Unit, interfaceUnit, operation);
        if (targetInstanceId is not null && targetInstanceId != 0)
        {
            var explicitAddress = Address(provider, targetInstanceId.Value);
            if (_instances.TryGetValue(explicitAddress, out var explicitInstance))
                return explicitInstance;
            throw new InvalidOperationException($"Box instance '{explicitAddress}' does not exist.");
        }

        var address = Address(provider, 0);
        if (_instances.TryGetValue(address, out var existing))
            return existing;

        BoxInstance created;
        lock (_gate)
        {
            if (_instances.TryGetValue(address, out existing))
                return existing;
            created = CreateInstance(provider, owner: null, frameworkOwned: true, defaultInstance: true);
        }
        await created.StartAsync();
        return created;
    }

    internal async Task<int> CreateBoxAsync(BoxInstance owner, string unit)
    {
        if (!_composition.Boxes.ContainsKey(unit))
            throw new InvalidOperationException($"App does not include box '{unit}'.");
        var instance = CreateInstance(unit, owner, frameworkOwned: false, defaultInstance: false);
        await instance.StartAsync();
        return instance.Id;
    }

    internal BoxInstance RequireOwnedInstance(BoxInstance owner, string address)
    {
        lock (owner.ChildGate)
        {
            return owner.Children.FirstOrDefault(child => child.Address == address)
                ?? throw new InvalidOperationException($"Box '{address}' is not owned by '{owner.Address}'.");
        }
    }

    internal bool TryGetInstance(string address, out BoxInstance instance) =>
        _instances.TryGetValue(address, out instance!);

    internal JsonNode? GetConfigItem(string key)
    {
        if (!_composition.Configuration.TryGetPropertyValue(key, out var value))
            throw new KeyNotFoundException($"Application configuration has no key '{key}'.");
        return value?.DeepClone();
    }

    internal void RequestShutdown()
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 0)
            _ = Task.Run(ShutdownAsync);
    }

    internal void RemoveInstance(BoxInstance instance)
    {
        _instances.TryRemove(instance.Address, out _);
        if (instance.Owner is { } owner)
        {
            lock (owner.ChildGate)
                owner.Children.Remove(instance);
        }
        else
        {
            lock (_gate)
                _frameworkOwned.Remove(instance);
        }
    }

    private BoxInstance CreateInstance(string unit, BoxInstance? owner, bool frameworkOwned, bool defaultInstance)
    {
        if (!_implementations.TryGetValue(unit, out var implementation))
            throw new InvalidOperationException($"No implementation is registered for box '{unit}'.");
        int id;
        lock (_gate)
        {
            if (defaultInstance)
            {
                id = 0;
                if (_instances.ContainsKey(Address(unit, id)))
                    throw new InvalidOperationException($"Default instance for '{unit}' already exists.");
                if (!_nextInstanceId.ContainsKey(unit))
                    _nextInstanceId[unit] = 1;
            }
            else
            {
                if (!_nextInstanceId.TryGetValue(unit, out id))
                    id = 1;
                _nextInstanceId[unit] = id + 1;
            }
        }

        var instance = new BoxInstance(this, _composition.Boxes[unit], implementation, id, owner);
        if (!_instances.TryAdd(instance.Address, instance))
            throw new InvalidOperationException($"Box instance '{instance.Address}' already exists.");
        if (frameworkOwned)
        {
            lock (_gate)
                _frameworkOwned.Add(instance);
        }
        else
        {
            lock (owner!.ChildGate)
                owner.Children.Add(instance);
        }
        return instance;
    }

    private async Task ShutdownAsync()
    {
        try
        {
            if (_instances.TryGetValue(Address(_composition.EntryBox, 0), out var entry))
                await entry.DestroyAsync();
            List<BoxInstance> defaults;
            lock (_gate)
                defaults = _frameworkOwned.ToList();
            for (var index = defaults.Count - 1; index >= 0; index--)
            {
                try
                {
                    await defaults[index].DestroyAsync();
                }
                catch (Exception exception)
                {
                    Log.Emit(LogCategory.Error, defaults[index].Unit, defaults[index].Id, $"shutdown failed: {exception}");
                }
            }
        }
        finally
        {
            _shutdownCompletion.TrySetResult();
        }
    }

    private T GetRuntimeValue<T>(string key, T defaultValue)
    {
        if (!_composition.Configuration.TryGetPropertyValue(key, out var node) || node is null)
            return defaultValue;
        return node.GetValue<T>();
    }

    private void ValidateRuntimeConfiguration()
    {
        ValidateRuntimeValue("runtime.defaultMsgTimeoutMs", new JsonObject
        {
            ["type"] = "integer",
            ["minimum"] = 1
        });
        ValidateRuntimeValue("runtime.destroyTimeoutMs", new JsonObject
        {
            ["type"] = "integer",
            ["minimum"] = 1
        });
        ValidateRuntimeValue("runtime.logLateResponses", new JsonObject { ["type"] = "boolean" });
        ValidateRuntimeValue("runtime.sendRemoteExceptionStacks", new JsonObject { ["type"] = "boolean" });
    }

    private void ValidateRuntimeValue(string key, JsonObject schema)
    {
        if (!_composition.Configuration.TryGetPropertyValue(key, out var value))
            return;
        if (!SchemaValidator.Validate(schema, value, out var error))
            throw new InvalidOperationException($"Runtime configuration item '{key}' is invalid: {error}.");
    }

    private static Dictionary<string, Type> DiscoverImplementations(IEnumerable<Assembly> assemblies)
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in assemblies.SelectMany(assembly => assembly.GetTypes()))
        {
            var attribute = type.GetCustomAttribute<BoxImplementationAttribute>();
            if (attribute is null)
                continue;
            if (!typeof(Box).IsAssignableFrom(type) || type.IsAbstract)
                throw new InvalidOperationException($"Implementation '{type.FullName}' must derive from Box.");
            if (!result.TryAdd(attribute.Unit, type))
                throw new InvalidOperationException($"Multiple implementations declare box '{attribute.Unit}'.");
        }
        return result;
    }

    private static string Address(string unit, int id) => $"{unit}|{id}";
}

internal sealed class BoxInstance
{
    private readonly Framework _framework;
    private readonly BoxContract _contract;
    private readonly Box _implementation;
    private readonly Dictionary<(string InterfaceUnit, string Operation), HandlerMethod> _handlers;
    private readonly Channel<RoutedMessage> _inbox = Channel.CreateUnbounded<RoutedMessage>();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ResponsePayload>> _pending =
        new(StringComparer.Ordinal);
    private readonly HashSet<Task> _runningHandlers = [];
    private readonly object _stateGate = new();
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _dispatcher;
    private BoxState _state = BoxState.Init;
    private int _destroyRequested;

    public BoxInstance(Framework framework, BoxContract contract, Type implementationType, int id, BoxInstance? owner)
    {
        _framework = framework;
        _contract = contract;
        Unit = contract.Unit;
        Id = id;
        Address = $"{Unit}|{Id}";
        Owner = owner;
        _implementation = (Box)(Activator.CreateInstance(implementationType)
            ?? throw new InvalidOperationException($"Cannot create '{implementationType.FullName}'."));
        _implementation.Context = new BoxContext(this);
        _handlers = ReadHandlers(implementationType);
    }

    public string Unit { get; }
    public int Id { get; }
    public string Address { get; }
    public BoxInstance? Owner { get; }
    public List<BoxInstance> Children { get; } = [];
    public object ChildGate { get; } = new();
    public bool IsCancelled => _cancellation.IsCancellationRequested;

    public async Task StartAsync()
    {
        try
        {
            await _implementation.InitAsync();
            lock (_stateGate)
                _state = BoxState.Running;
        }
        catch (Exception exception)
        {
            lock (_stateGate)
                _state = BoxState.Error;
            _framework.Log.Emit(LogCategory.Error, Unit, Id, $"init failed: {exception}");
        }
        finally
        {
            // Messages can arrive during INIT, but are not consumed until init resolves.
            _dispatcher = Task.Run(DispatchLoopAsync);
        }
    }

    public void DispatchRun()
    {
        if (State != BoxState.Running)
        {
            _framework.RequestShutdown();
            return;
        }
        Track(Task.Run(async () =>
        {
            try
            {
                await _implementation.RunAsync();
            }
            catch (Exception exception)
            {
                lock (_stateGate)
                    if (_state == BoxState.Running)
                        _state = BoxState.Error;
                _framework.Log.Emit(LogCategory.Error, Unit, Id, $"run failed: {exception}");
                _framework.RequestShutdown();
            }
        }));
    }

    public void Enqueue(RoutedMessage message) => _inbox.Writer.TryWrite(message);

    public Task<ResponsePayload> WaitForResponse(string requestId, int timeoutMs)
    {
        var completion = new TaskCompletionSource<ResponsePayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = completion;
        _ = TimeoutAsync();
        return completion.Task;

        async Task TimeoutAsync()
        {
            await Task.Delay(timeoutMs);
            if (_pending.TryRemove(requestId, out var timedOut))
                timedOut.TrySetException(new TimeoutException($"REQ id={requestId} timed out after {timeoutMs}ms."));
        }
    }

    public void CompleteResponse(string requestId, ResponsePayload payload)
    {
        if (_pending.TryRemove(requestId, out var completion))
        {
            completion.TrySetResult(payload);
        }
        else if (_framework.LogLateResponses)
        {
            _framework.Log.Emit(LogCategory.Warning, Unit, Id, $"late response id={requestId} discarded");
        }
    }

    public async Task DestroyAsync()
    {
        if (Interlocked.Exchange(ref _destroyRequested, 1) != 0)
            return;
        lock (_stateGate)
            _state = BoxState.Deinit;
        _cancellation.Cancel();

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(_framework.DestroyTimeoutMs);
        Task[] handlers;
        lock (_stateGate)
            handlers = _runningHandlers.ToArray();
        var drained = await BeforeDeadlineAsync(Task.WhenAll(handlers), deadline);
        if (drained)
        {
            try
            {
                if (!await BeforeDeadlineAsync(_implementation.DeinitAsync(), deadline))
                    _framework.Log.Emit(LogCategory.Error, Unit, Id, "deinit timed out; forcing removal");
            }
            catch (Exception exception)
            {
                _framework.Log.Emit(LogCategory.Error, Unit, Id, $"deinit failed: {exception}");
            }
        }
        else
        {
            _framework.Log.Emit(LogCategory.Error, Unit, Id, "destruction timed out before handlers drained; deinit skipped");
        }

        List<BoxInstance> children;
        lock (ChildGate)
            children = Children.ToList();
        for (var index = children.Count - 1; index >= 0; index--)
            await children[index].DestroyAsync();

        _inbox.Writer.TryComplete();
        if (_dispatcher is not null)
            await _dispatcher;
        _framework.RemoveInstance(this);
    }

    internal Framework Framework => _framework;

    private BoxState State
    {
        get
        {
            lock (_stateGate)
                return _state;
        }
    }

    private async Task DispatchLoopAsync()
    {
        try
        {
            await foreach (var message in _inbox.Reader.ReadAllAsync())
            {
                try
                {
                    HandleInbound(message);
                }
                catch (Exception exception)
                {
                    SetDispatcherError(exception);
                    Reject(message, "box-error");
                }
            }
        }
        catch (Exception exception)
        {
            SetDispatcherError(exception);
        }
    }

    private void HandleInbound(RoutedMessage message)
    {
        var state = State;
        if (state == BoxState.Deinit)
        {
            Reject(message, "box-deinitializing");
            return;
        }
        if (state == BoxState.Error)
        {
            Reject(message, "box-error");
            return;
        }
        if (state != BoxState.Running ||
            !_contract.Provides.Contains((message.InterfaceUnit, message.Operation)))
        {
            Reject(message, "unexpected-message-format");
            return;
        }

        var contract = _framework.GetOperation(message.InterfaceUnit, message.Operation);
        if (!SchemaValidator.Validate(contract.InputSchema, message.Payload, out var inputError))
        {
            if (message.Method == MessageMethod.Once)
                _framework.Log.Emit(LogCategory.Warning, Unit, Id, $"unexpected-message-format: {inputError}");
            Reject(message, "unexpected-message-format");
            return;
        }
        if (!_handlers.TryGetValue((message.InterfaceUnit, message.Operation), out var handler))
        {
            Reject(message, "unexpected-message-format");
            return;
        }
        Track(Task.Run(() => InvokeHandlerAsync(message, contract, handler)));
    }

    private async Task InvokeHandlerAsync(
        RoutedMessage message,
        OperationContract contract,
        HandlerMethod handler)
    {
        object? payload;
        try
        {
            payload = message.Payload is null
                ? null
                : message.Payload.Deserialize(handler.PayloadType, Framework.JsonOptions);
            var task = (Task)handler.Method.Invoke(_implementation, [payload])!;
            await task;
            var result = handler.ResultType is null ? null : task.GetType().GetProperty("Result")!.GetValue(task);
            if (message.Method == MessageMethod.Once)
                return;

            JsonNode? resultNode = result is null
                ? null
                : JsonSerializer.SerializeToNode(result, handler.ResultType!, Framework.JsonOptions);
            if (contract.ResponseSchema is null)
            {
                if (resultNode is not null)
                    throw new InvalidOperationException("Handler returned a result for an operation with a null response schema.");
            }
            else if (!SchemaValidator.Validate(contract.ResponseSchema, resultNode, out var error))
            {
                _framework.Log.Emit(LogCategory.Error, Unit, Id, $"{contract.InterfaceUnit}.{contract.Name} response: {error}");
                SendResponse(message, ResponseStatus.Exception, "unexpected-response-format", null);
                return;
            }
            SendResponse(message, ResponseStatus.Ok, null, resultNode);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationError error)
        {
            SendOperationError(message, contract, error);
        }
        catch (OperationError error)
        {
            SendOperationError(message, contract, error);
        }
        catch (Exception exception)
        {
            var actual = exception is TargetInvocationException wrapper && wrapper.InnerException is not null
                ? wrapper.InnerException
                : exception;
            if (message.Method == MessageMethod.Once)
            {
                _framework.Log.Emit(LogCategory.Error, Unit, Id, $"{contract.InterfaceUnit}.{contract.Name} failed: {actual}");
                return;
            }
            var text = _framework.SendRemoteExceptionStacks
                ? $"{actual.GetType().Name}: {actual.Message}\nRemote stack:\n{actual.StackTrace}"
                : $"{actual.GetType().Name}: {actual.Message}";
            SendResponse(message, ResponseStatus.Exception, text, null);
        }
    }

    private void SendOperationError(RoutedMessage message, OperationContract contract, OperationError error)
    {
        if (message.Method == MessageMethod.Once)
        {
            _framework.Log.Emit(LogCategory.Error, Unit, Id, $"{contract.InterfaceUnit}.{contract.Name} error: {error.Failure}");
            return;
        }
        if (!contract.Failures.Contains(error.Failure))
        {
            SendResponse(message, ResponseStatus.Exception, $"undeclared-operation-error: {error.Failure}", null);
            return;
        }
        SendResponse(message, ResponseStatus.Error, error.Failure, null);
    }

    private void Reject(RoutedMessage message, string text)
    {
        if (message.Method == MessageMethod.Req)
            SendResponse(message, ResponseStatus.Exception, text, null);
        else
            _framework.Log.Emit(LogCategory.Warning, Unit, Id, $"{text}: rejected {message.InterfaceUnit}.{message.Operation}");
    }

    private void SendResponse(RoutedMessage request, ResponseStatus status, string? text, JsonNode? result)
    {
        if (_framework.TryGetInstance(request.Sender, out var sender))
            sender.CompleteResponse(request.Id, new ResponsePayload { Status = status, Text = text, Result = result });
    }

    private void SetDispatcherError(Exception exception)
    {
        lock (_stateGate)
        {
            if (_state != BoxState.Deinit)
                _state = BoxState.Error;
        }
        _framework.Log.Emit(LogCategory.Error, Unit, Id, $"dispatcher failed: {exception}");
    }

    private void Track(Task task)
    {
        lock (_stateGate)
            _runningHandlers.Add(task);
        _ = task.ContinueWith(completed =>
        {
            lock (_stateGate)
                _runningHandlers.Remove(completed);
        }, TaskScheduler.Default);
    }

    private static async Task<bool> BeforeDeadlineAsync(Task task, DateTimeOffset deadline)
    {
        var remaining = deadline - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return task.IsCompletedSuccessfully;
        var completed = await Task.WhenAny(task, Task.Delay(remaining));
        if (completed != task)
            return false;
        await task;
        return true;
    }

    private static Dictionary<(string, string), HandlerMethod> ReadHandlers(Type implementationType)
    {
        var result = new Dictionary<(string, string), HandlerMethod>();
        foreach (var method in implementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = method.GetCustomAttribute<OperationHandlerAttribute>();
            if (attribute is null)
                continue;
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{implementationType.Name}.{method.Name}' must accept one payload.");
            var returnType = method.ReturnType;
            Type? resultType;
            if (returnType == typeof(Task))
                resultType = null;
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                resultType = returnType.GetGenericArguments()[0];
            else
                throw new InvalidOperationException($"Handler '{implementationType.Name}.{method.Name}' must return Task or Task<T>.");
            result.Add((attribute.InterfaceUnit, attribute.Operation), new HandlerMethod(method, parameters[0].ParameterType, resultType));
        }
        return result;
    }
}

internal sealed class BoxContext : IBoxContext
{
    private readonly BoxInstance _self;

    public BoxContext(BoxInstance self) => _self = self;

    public string SelfAddress => _self.Address;
    public bool IsCancelled => _self.IsCancelled;

    public async Task<ResponsePayload> RequestAsync(
        string interfaceUnit,
        string operation,
        object? payload,
        int? targetInstanceId = null,
        int? timeoutMs = null)
    {
        var contract = _self.Framework.GetOperation(interfaceUnit, operation);
        if (!contract.ExpectsResponse)
            throw new InvalidOperationException($"{interfaceUnit}.{operation} does not accept REQ.");
        var target = await _self.Framework.ResolveTargetAsync(_self, interfaceUnit, operation, targetInstanceId);
        var id = Guid.NewGuid().ToString("N");
        var message = new RoutedMessage(
            target.Address,
            _self.Address,
            MessageMethod.Req,
            interfaceUnit,
            operation,
            id,
            Serialize(payload));
        var waiting = _self.WaitForResponse(id, timeoutMs ?? _self.Framework.DefaultMessageTimeoutMs);
        target.Enqueue(message);
        return await waiting;
    }

    public async Task SendOnceAsync(
        string interfaceUnit,
        string operation,
        object? payload,
        int? targetInstanceId = null)
    {
        var contract = _self.Framework.GetOperation(interfaceUnit, operation);
        if (contract.ExpectsResponse)
            throw new InvalidOperationException($"{interfaceUnit}.{operation} requires REQ.");
        var target = await _self.Framework.ResolveTargetAsync(_self, interfaceUnit, operation, targetInstanceId);
        target.Enqueue(new RoutedMessage(
            target.Address,
            _self.Address,
            MessageMethod.Once,
            interfaceUnit,
            operation,
            Guid.NewGuid().ToString("N"),
            Serialize(payload)));
    }

    public Task<int> CreateBoxAsync(string unit) => _self.Framework.CreateBoxAsync(_self, unit);

    public Task DestroyAsync(string address) => _self.Framework.RequireOwnedInstance(_self, address).DestroyAsync();

    public JsonNode? GetConfigItem(string key) => _self.Framework.GetConfigItem(key);

    public T? GetConfigItem<T>(string key)
    {
        var value = GetConfigItem(key);
        return value is null ? default : value.Deserialize<T>(Framework.JsonOptions);
    }

    public void Shutdown() => _self.Framework.RequestShutdown();

    public void Log(LogCategory category, string content) =>
        _self.Framework.Log.Emit(category, _self.Unit, _self.Id, content);

    private static JsonNode? Serialize(object? value) =>
        value is null ? null : JsonSerializer.SerializeToNode(value, value.GetType(), Framework.JsonOptions);
}
