using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Mbox;

internal sealed record UnitHeader(string Unit, string Type, int Version, IReadOnlyDictionary<string, int> Uses);

internal sealed record UnitDocument(string Path, UnitHeader Header, JsonObject? Definition);

internal sealed class UnitCatalog
{
    private readonly IReadOnlyDictionary<string, UnitDocument> _units;

    private UnitCatalog(string repositoryRoot, IReadOnlyDictionary<string, UnitDocument> units)
    {
        RepositoryRoot = repositoryRoot;
        _units = units;
    }

    public string RepositoryRoot { get; }

    public UnitDocument Get(string unit) =>
        _units.TryGetValue(unit, out var document)
            ? document
            : throw new InvalidOperationException($"Unit '{unit}' could not be discovered.");

    public UnitDocument GetRequiredUse(UnitDocument dependent, string dependency)
    {
        var document = Get(dependency);
        if (!dependent.Header.Uses.TryGetValue(dependency, out var version))
            throw new InvalidOperationException(
                $"Unit '{dependent.Header.Unit}' references '{dependency}' without declaring it in uses.");
        if (version != document.Header.Version)
            throw new InvalidOperationException(
                $"Unit '{dependent.Header.Unit}' uses stale '{dependency}:{version}'; current version is {document.Header.Version}.");
        return document;
    }

    public void ValidateDependencyClosure(UnitDocument root)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        Visit(root);

        void Visit(UnitDocument dependent)
        {
            if (!visited.Add(dependent.Header.Unit))
                return;
            foreach (var dependency in dependent.Header.Uses.Keys)
                Visit(GetRequiredUse(dependent, dependency));
        }
    }

    public static UnitCatalog Discover(string appUnitPath)
    {
        var fullAppPath = Path.GetFullPath(appUnitPath);
        var root = FindRepositoryRoot(Path.GetDirectoryName(fullAppPath)!);
        var byUnit = new Dictionary<string, UnitDocument>(StringComparer.Ordinal);
        var unitsRoot = Path.Combine(root, "units");
        if (Directory.Exists(unitsRoot))
        {
            foreach (var file in Directory.EnumerateFiles(unitsRoot, "*.md", SearchOption.AllDirectories))
            {
                var document = UnitYaml.Read(file);
                if (document is null)
                    continue;
                if (!byUnit.TryAdd(document.Header.Unit, document))
                    throw new InvalidOperationException($"Duplicate unit identifier '{document.Header.Unit}'.");
            }
        }
        return new UnitCatalog(root, byUnit);
    }

    private static string FindRepositoryRoot(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "units", "system", "kernel.v1.md")))
                return current.FullName;
        }
        throw new InvalidOperationException("Unable to find a repository containing units/system/kernel.v1.md.");
    }
}

internal static partial class UnitYaml
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build();

    public static UnitDocument? Read(string path)
    {
        var content = File.ReadAllText(path);
        var headerMatch = HeaderRegex().Match(content);
        if (!headerMatch.Success)
            return null;
        var header = ToJson(Deserializer.Deserialize<object>(headerMatch.Groups[1].Value)) as JsonObject
            ?? throw new InvalidOperationException($"Invalid unit header in '{path}'.");
        if (header["mbox_unit"]?.GetValue<int>() != 1)
            return null;
        var unit = header["unit"]?.GetValue<string>()
            ?? throw new InvalidOperationException($"Unit header has no unit identifier in '{path}'.");
        var type = header["type"]?.GetValue<string>()
            ?? throw new InvalidOperationException($"Unit header has no type in '{path}'.");
        var version = header["version"]?.GetValue<int>()
            ?? throw new InvalidOperationException($"Unit header has no version in '{path}'.");
        var uses = new Dictionary<string, int>(StringComparer.Ordinal);
        if (header["uses"] is JsonObject useMap)
            foreach (var item in useMap)
                uses[item.Key] = item.Value!.GetValue<int>();

        JsonObject? definition = null;
        var definitionMatch = DefinitionRegex().Match(content);
        if (definitionMatch.Success)
            definition = ToJson(Deserializer.Deserialize<object>(definitionMatch.Groups[1].Value)) as JsonObject
                ?? throw new InvalidOperationException($"Invalid definition block in '{path}'.");
        return new UnitDocument(path, new UnitHeader(unit, type, version, uses), definition);
    }

    public static JsonNode? ToJson(object? value)
    {
        return value switch
        {
            null => null,
            IDictionary<object, object> map => ToObject(map),
            IEnumerable<object> sequence when value is not string => new JsonArray(sequence.Select(ToJson).ToArray()),
            bool boolean => JsonValue.Create(boolean),
            byte number => JsonValue.Create((int)number),
            sbyte number => JsonValue.Create((int)number),
            short number => JsonValue.Create((int)number),
            ushort number => JsonValue.Create((int)number),
            int number => JsonValue.Create(number),
            uint number => JsonValue.Create((long)number),
            long number => JsonValue.Create(number),
            ulong number when number <= long.MaxValue => JsonValue.Create((long)number),
            float number => JsonValue.Create((double)number),
            double number => JsonValue.Create(number),
            decimal number => JsonValue.Create(number),
            string text => JsonValue.Create(text),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static JsonObject ToObject(IDictionary<object, object> mapping)
    {
        var result = new JsonObject();
        foreach (var field in mapping)
            result[field.Key.ToString()!] = ToJson(field.Value);
        return result;
    }

    [GeneratedRegex(@"\A---\s*\r?\n(.*?)\r?\n---\s*\r?\n", RegexOptions.Singleline)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^## Definition\s*\r?\n\s*```yaml\s*\r?\n(.*?)\r?\n```", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex DefinitionRegex();
}
