using System.Text.Json.Nodes;

namespace Mbox;

internal sealed record OperationContract(
    string InterfaceUnit,
    string Name,
    bool ExpectsResponse,
    JsonNode InputSchema,
    JsonNode? ResponseSchema,
    IReadOnlySet<string> Failures);

internal sealed record BoxContract(
    string Unit,
    IReadOnlySet<(string InterfaceUnit, string Operation)> Provides,
    IReadOnlySet<(string InterfaceUnit, string Operation)> Consumes,
    JsonObject Configuration);

internal sealed class Composition
{
    private readonly Dictionary<(string Consumer, string InterfaceUnit, string Operation), string> _routes;

    private Composition(
        string appName,
        string entryBox,
        Dictionary<string, BoxContract> boxes,
        Dictionary<(string InterfaceUnit, string Operation), OperationContract> operations,
        Dictionary<(string Consumer, string InterfaceUnit, string Operation), string> routes,
        JsonObject configuration)
    {
        AppName = appName;
        EntryBox = entryBox;
        Boxes = boxes;
        Operations = operations;
        _routes = routes;
        Configuration = configuration;
    }

    public string AppName { get; }
    public string EntryBox { get; }
    public IReadOnlyDictionary<string, BoxContract> Boxes { get; }
    public IReadOnlyDictionary<(string InterfaceUnit, string Operation), OperationContract> Operations { get; }
    public JsonObject Configuration { get; }

    public string ResolveProvider(string consumer, string interfaceUnit, string operation) =>
        _routes.TryGetValue((consumer, interfaceUnit, operation), out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"No app binding routes {consumer}'s consumption of {interfaceUnit}.{operation}.");

    public OperationContract GetOperation(string interfaceUnit, string operation) =>
        Operations.TryGetValue((interfaceUnit, operation), out var contract)
            ? contract
            : throw new InvalidOperationException($"Unknown operation '{interfaceUnit}.{operation}'.");

    public static Composition Load(string appUnitPath, JsonObject applicationConfiguration)
    {
        var catalog = UnitCatalog.Discover(appUnitPath);
        var app = UnitYaml.Read(appUnitPath)
            ?? throw new InvalidOperationException($"'{appUnitPath}' is not an MBOX unit.");
        if (app.Header.Type != "app" || app.Definition is null)
            throw new InvalidOperationException($"'{appUnitPath}' is not an app unit with a definition.");
        catalog.ValidateDependencyClosure(app);

        var definition = app.Definition;
        var entryBox = RequiredString(definition, "entryBox");
        var boxNames = RequiredArray(definition, "boxes").Select(node => node!.GetValue<string>()).ToArray();
        var bindings = RequiredArray(definition, "bindings");
        var externalProviders = RequiredArray(definition, "externalProviders");
        var exposes = RequiredArray(definition, "exposes");
        var declaredConfiguration = RequiredObject(definition, "configuration");
        if (!boxNames.Contains(entryBox, StringComparer.Ordinal))
            throw new InvalidOperationException($"Entry box '{entryBox}' is not included by app '{app.Header.Unit}'.");

        var operations = new Dictionary<(string, string), OperationContract>();
        var boxes = boxNames.ToDictionary(
            unit => unit,
            unit => ReadBox(catalog, app, unit, operations),
            StringComparer.Ordinal);
        var routes = new Dictionary<(string, string, string), string>();

        foreach (var bindingNode in bindings)
        {
            var binding = bindingNode as JsonObject
                ?? throw new InvalidOperationException("An app binding is not an object.");
            var consumer = RequiredString(binding, "consumer");
            var provider = RequiredString(binding, "provider");
            var interfaceUnit = RequiredString(binding, "interface");
            if (!boxes.ContainsKey(consumer) || !boxes.ContainsKey(provider))
                throw new InvalidOperationException($"Binding '{consumer}' -> '{provider}' names a box outside the app.");
            ReadInterfaceOperations(catalog.GetRequiredUse(app, interfaceUnit), operations);
            foreach (var operationNode in RequiredArray(binding, "operations"))
            {
                var operation = operationNode!.GetValue<string>();
                var key = (interfaceUnit, operation);
                if (!boxes[consumer].Consumes.Contains(key))
                    throw new InvalidOperationException($"Box '{consumer}' does not consume {interfaceUnit}.{operation}.");
                if (!boxes[provider].Provides.Contains(key))
                    throw new InvalidOperationException($"Box '{provider}' does not provide {interfaceUnit}.{operation}.");
                if (!routes.TryAdd((consumer, interfaceUnit, operation), provider))
                    throw new InvalidOperationException($"Operation {interfaceUnit}.{operation} is bound more than once for '{consumer}'.");
            }
        }

        if (externalProviders.Count != 0)
            throw new NotSupportedException("mbox-dotnet v1 does not yet host external provider bindings.");

        foreach (var exposedNode in exposes)
        {
            var exposed = exposedNode as JsonObject
                ?? throw new InvalidOperationException("An app exposure is not an object.");
            var provider = RequiredString(exposed, "provider");
            var interfaceUnit = RequiredString(exposed, "interface");
            if (!boxes.ContainsKey(provider))
                throw new InvalidOperationException($"Exposure provider '{provider}' names a box outside the app.");
            ReadInterfaceOperations(catalog.GetRequiredUse(app, interfaceUnit), operations);
            foreach (var operationNode in RequiredArray(exposed, "operations"))
            {
                var operation = operationNode!.GetValue<string>();
                if (!operations.ContainsKey((interfaceUnit, operation)))
                    throw new InvalidOperationException($"Exposure names undefined operation {interfaceUnit}.{operation}.");
                if (!boxes[provider].Provides.Contains((interfaceUnit, operation)))
                    throw new InvalidOperationException($"Box '{provider}' does not provide exposed operation {interfaceUnit}.{operation}.");
            }
        }
        if (exposes.Count != 0)
            throw new NotSupportedException("mbox-dotnet v1 does not yet host externally exposed app operations.");

        foreach (var box in boxes.Values)
            foreach (var consumed in box.Consumes)
                if (!routes.ContainsKey((box.Unit, consumed.InterfaceUnit, consumed.Operation)))
                    throw new InvalidOperationException(
                        $"App does not bind {box.Unit}'s consumed operation {consumed.InterfaceUnit}.{consumed.Operation}.");

        var configuration = new JsonObject();
        foreach (var item in declaredConfiguration)
            configuration[item.Key] = item.Value?.DeepClone();
        foreach (var item in applicationConfiguration)
            configuration[item.Key] = item.Value?.DeepClone();

        ValidateConfiguration(boxes.Values, configuration);
        return new Composition(app.Header.Unit, entryBox, boxes, operations, routes, configuration);
    }

    private static BoxContract ReadBox(
        UnitCatalog catalog,
        UnitDocument app,
        string unit,
        Dictionary<(string, string), OperationContract> operations)
    {
        var document = catalog.GetRequiredUse(app, unit);
        if (document.Header.Type != "box" || document.Definition is null)
            throw new InvalidOperationException($"Unit '{document.Header.Unit}' is not a box definition.");
        var provides = ReadOperationReferences(document.Definition, "provides");
        var consumes = ReadOperationReferences(document.Definition, "consumes");
        var config = document.Definition["configuration"] as JsonObject
            ?? throw new InvalidOperationException($"Box '{document.Header.Unit}' has no configuration mapping.");
        ValidateOperationReferences(catalog, document, provides, operations);
        ValidateOperationReferences(catalog, document, consumes, operations);
        return new BoxContract(document.Header.Unit, provides, consumes, config);
    }

    private static void ValidateOperationReferences(
        UnitCatalog catalog,
        UnitDocument box,
        IEnumerable<(string InterfaceUnit, string Operation)> references,
        Dictionary<(string, string), OperationContract> operations)
    {
        foreach (var reference in references)
        {
            ReadInterfaceOperations(catalog.GetRequiredUse(box, reference.InterfaceUnit), operations);
            if (!operations.ContainsKey((reference.InterfaceUnit, reference.Operation)))
                throw new InvalidOperationException(
                    $"Box '{box.Header.Unit}' references undefined operation {reference.InterfaceUnit}.{reference.Operation}.");
        }
    }

    private static HashSet<(string InterfaceUnit, string Operation)> ReadOperationReferences(JsonObject definition, string key)
    {
        var references = new HashSet<(string, string)>();
        foreach (var itemNode in RequiredArray(definition, key))
        {
            var item = itemNode as JsonObject
                ?? throw new InvalidOperationException($"'{key}' item is not an object.");
            var interfaceUnit = RequiredString(item, "interface");
            foreach (var operationNode in RequiredArray(item, "operations"))
                references.Add((interfaceUnit, operationNode!.GetValue<string>()));
        }
        return references;
    }

    private static void ReadInterfaceOperations(
        UnitDocument document,
        Dictionary<(string, string), OperationContract> operations)
    {
        if (document.Header.Type != "interface" || document.Definition?["operations"] is not JsonObject definitions)
            throw new InvalidOperationException($"Unit '{document.Header.Unit}' is not an interface definition.");
        foreach (var item in definitions)
        {
            if (operations.ContainsKey((document.Header.Unit, item.Key)))
                continue;
            var operation = item.Value as JsonObject
                ?? throw new InvalidOperationException($"Operation '{document.Header.Unit}.{item.Key}' is invalid.");
            var failures = operation["failures"] is JsonObject failureMap
                ? failureMap.Select(field => field.Key).ToHashSet(StringComparer.Ordinal)
                : throw new InvalidOperationException($"Operation '{document.Header.Unit}.{item.Key}' has no failures map.");
            operations[(document.Header.Unit, item.Key)] = new OperationContract(
                document.Header.Unit,
                item.Key,
                operation["expectsResponse"]!.GetValue<bool>(),
                operation["input"]!.DeepClone(),
                operation["response"]?.DeepClone(),
                failures);
        }
    }

    private static void ValidateConfiguration(IEnumerable<BoxContract> boxes, JsonObject configuration)
    {
        foreach (var box in boxes)
        {
            foreach (var item in box.Configuration)
            {
                var declaration = item.Value as JsonObject
                    ?? throw new InvalidOperationException($"Configuration declaration '{box.Unit}.{item.Key}' is invalid.");
                var required = declaration["required"]!.GetValue<bool>();
                if (!configuration.TryGetPropertyValue(item.Key, out var value))
                {
                    if (required)
                        throw new InvalidOperationException($"Required configuration item '{item.Key}' for '{box.Unit}' is absent.");
                    continue;
                }
                if (!SchemaValidator.Validate(declaration["schema"], value, out var error))
                    throw new InvalidOperationException($"Configuration item '{item.Key}' for '{box.Unit}' is invalid: {error}.");
            }
        }
    }

    private static string RequiredString(JsonObject value, string key) =>
        value[key]?.GetValue<string>() ?? throw new InvalidOperationException($"Required string '{key}' is absent.");

    private static JsonArray RequiredArray(JsonObject value, string key) =>
        value[key] as JsonArray ?? throw new InvalidOperationException($"Required array '{key}' is absent.");

    private static JsonObject RequiredObject(JsonObject value, string key) =>
        value[key] as JsonObject ?? throw new InvalidOperationException($"Required object '{key}' is absent.");
}
