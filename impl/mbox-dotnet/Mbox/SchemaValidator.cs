using System.Text.Json.Nodes;

namespace Mbox;

internal static class SchemaValidator
{
    public static bool Validate(JsonNode? schema, JsonNode? value, out string? error)
    {
        if (schema is not JsonObject definition || definition["type"] is not JsonValue typeValue ||
            !typeValue.TryGetValue<string>(out var type))
        {
            error = "schema is missing type";
            return false;
        }

        switch (type)
        {
            case "any":
                error = null;
                return true;
            case "null":
                if (value is null)
                {
                    error = null;
                    return true;
                }
                error = "expected null";
                return false;
            case "boolean":
                if (value is JsonValue boolean && boolean.TryGetValue<bool>(out _))
                {
                    error = null;
                    return true;
                }
                error = "expected boolean";
                return false;
            case "integer":
                if (value is JsonValue integer && integer.TryGetValue<long>(out var whole))
                    return ValidateNumberRange(definition, whole, out error);
                error = "expected integer";
                return false;
            case "number":
                if (TryNumber(value, out var number))
                    return ValidateNumberRange(definition, number, out error);
                error = "expected number";
                return false;
            case "string":
                if (value is JsonValue textValue && textValue.TryGetValue<string>(out var text))
                    return ValidateLength(definition, text.EnumerateRunes().Count(), "minLength", "maxLength", out error);
                error = "expected string";
                return false;
            case "binary":
                if (value is JsonValue binaryValue && binaryValue.TryGetValue<string>(out var encoded))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(encoded);
                        return ValidateLength(definition, bytes.Length, "minBytes", "maxBytes", out error);
                    }
                    catch (FormatException)
                    {
                        error = "expected base64 encoded binary value";
                        return false;
                    }
                }
                error = "expected binary";
                return false;
            case "array":
                return ValidateArray(definition, value, out error);
            case "object":
                return ValidateObject(definition, value, out error);
            default:
                error = $"unknown schema type '{type}'";
                return false;
        }
    }

    private static bool ValidateArray(JsonObject schema, JsonNode? value, out string? error)
    {
        if (value is not JsonArray array)
        {
            error = "expected array";
            return false;
        }
        if (!ValidateLength(schema, array.Count, "minItems", "maxItems", out error))
            return false;
        if (schema["items"] is null)
        {
            error = "array schema has no items schema";
            return false;
        }
        for (var index = 0; index < array.Count; index++)
        {
            if (!Validate(schema["items"], array[index], out error))
            {
                error = $"items[{index}]: {error}";
                return false;
            }
        }
        error = null;
        return true;
    }

    private static bool ValidateObject(JsonObject schema, JsonNode? value, out string? error)
    {
        if (value is not JsonObject instance)
        {
            error = "expected object";
            return false;
        }
        if (schema["properties"] is not JsonObject properties ||
            schema["required"] is not JsonArray required ||
            schema["additionalProperties"] is not JsonValue additional ||
            !additional.TryGetValue<bool>(out var acceptsAdditional))
        {
            error = "invalid object schema";
            return false;
        }
        foreach (var item in required)
        {
            var name = item!.GetValue<string>();
            if (!instance.ContainsKey(name))
            {
                error = $"missing required '{name}'";
                return false;
            }
        }
        foreach (var field in instance)
        {
            if (properties[field.Key] is { } propertySchema)
            {
                if (!Validate(propertySchema, field.Value, out error))
                {
                    error = $"{field.Key}: {error}";
                    return false;
                }
            }
            else if (!acceptsAdditional)
            {
                error = $"unexpected property '{field.Key}'";
                return false;
            }
        }
        error = null;
        return true;
    }

    private static bool ValidateNumberRange(JsonObject schema, double value, out string? error)
    {
        if (TryNumber(schema["minimum"], out var minimum) && value < minimum)
        {
            error = $"value is below minimum {minimum}";
            return false;
        }
        if (TryNumber(schema["maximum"], out var maximum) && value > maximum)
        {
            error = $"value is above maximum {maximum}";
            return false;
        }
        error = null;
        return true;
    }

    private static bool ValidateLength(
        JsonObject schema,
        int length,
        string minimumKey,
        string maximumKey,
        out string? error)
    {
        if (schema[minimumKey] is JsonValue minimumValue && minimumValue.TryGetValue<int>(out var minimum) &&
            length < minimum)
        {
            error = $"length is below {minimumKey} {minimum}";
            return false;
        }
        if (schema[maximumKey] is JsonValue maximumValue && maximumValue.TryGetValue<int>(out var maximum) &&
            length > maximum)
        {
            error = $"length is above {maximumKey} {maximum}";
            return false;
        }
        error = null;
        return true;
    }

    private static bool TryNumber(JsonNode? value, out double number)
    {
        if (value is JsonValue numeric && numeric.TryGetValue<double>(out number))
            return true;
        number = default;
        return false;
    }
}
