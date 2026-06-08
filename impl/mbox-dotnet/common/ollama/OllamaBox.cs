using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("ollama")]
public sealed class OllamaBox : Box
{
    public sealed record GenerateInput(
        string BaseUrl,
        string Model,
        string Prompt,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        JsonNode? Format = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        double? Temperature = null);
    public sealed record GenerateResult(string Model, string Response);

    private sealed record GenerateApiResponse(string Model, string Response);
    private sealed record ErrorApiResponse(string? Error);

    private static readonly HttpClient Http = new();

    [OperationHandler("ollama-api", "generate")]
    public async Task<GenerateResult> Generate(GenerateInput input)
    {
        if (!Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new OperationError("invalid-base-url");
        }

        var uri = new Uri($"{baseUri.ToString().TrimEnd('/')}/api/generate");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var request = new JsonObject
        {
            ["model"] = input.Model,
            ["prompt"] = input.Prompt,
            ["stream"] = false
        };
        if (input.Format is not null)
            request["format"] = input.Format.DeepClone();
        if (input.Temperature is not null)
            request["options"] = new JsonObject { ["temperature"] = input.Temperature.Value };

        using var response = await Http.PostAsJsonAsync(
            uri,
            request,
            cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var failure = await response.Content.ReadFromJsonAsync<ErrorApiResponse>(
                cancellationToken: cts.Token);
            var detail = failure?.Error ?? response.ReasonPhrase ?? "request failed";
            Context.Log(LogCategory.Warning,
                $"ollama HTTP {(int)response.StatusCode}: {detail}");
            throw new OperationError("ollama-http-error");
        }

        var result = await response.Content.ReadFromJsonAsync<GenerateApiResponse>(
            cancellationToken: cts.Token)
            ?? throw new InvalidOperationException("Ollama returned an empty response body.");

        return new GenerateResult(result.Model, result.Response);
    }
}
