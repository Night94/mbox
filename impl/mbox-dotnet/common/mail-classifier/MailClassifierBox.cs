using System.Text;
using System.Text.Json;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("mail-classifier")]
public sealed class MailClassifierBox : Box
{
    public sealed record ClassifyInput(
        string Folder, long Uid, long UidValidity,
        string From, string To, string Subject, string Date, string BodyText);

    public sealed record ClassifyResult(string Folder);

    private sealed record GenerateResult(string Model, string Response);

    [OperationHandler("mail-classifier-api", "classify")]
    public async Task<ClassifyResult> Classify(ClassifyInput message)
    {
        var rules = Context.GetConfigItem<string[]>("Classifier.Rules")
            ?? throw new InvalidOperationException("Classifier.Rules is missing.");

        for (var index = 0; index < rules.Length; index++)
        {
            var rule = rules[index];
            var parts = rule.Split(
                (char[]?)null,
                2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                throw new InvalidOperationException($"Classifier.Rules[{index}] is malformed.");

            if (parts[0].Equals("MATCH", StringComparison.OrdinalIgnoreCase))
            {
                var folder = Match(message, parts[1], index);
                if (folder is not null)
                    return new ClassifyResult(folder);
                continue;
            }

            if (parts[0].Equals("ASK", StringComparison.OrdinalIgnoreCase))
            {
                var folder = await Ask(message, parts[1], index);
                if (folder is not null)
                    return new ClassifyResult(folder);
                continue;
            }

            throw new InvalidOperationException(
                $"Classifier.Rules[{index}] uses unsupported command '{parts[0]}'.");
        }

        throw new OperationError("no-matching-rule");
    }

    private static string? Match(ClassifyInput message, string arguments, int index)
    {
        var (folder, matchArguments) = ReadFolder(arguments, index, "MATCH");
        var parts = matchArguments.Split(
            (char[]?)null,
            2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Classifier.Rules[{index}] MATCH is malformed.");

        var headerValue = GetHeaderValue(message, parts[0]);
        return headerValue.Contains(parts[1], StringComparison.OrdinalIgnoreCase)
            ? folder
            : null;
    }

    private async Task<string?> Ask(ClassifyInput message, string arguments, int index)
    {
        var (folder, criterion) = ReadFolder(arguments, index, "ASK");
        var baseUrl = ReadRequiredString("ollama.baseUrl");
        var model = ReadRequiredString("ollama.model");
        var prompt = BuildAskPrompt(message, criterion);
        var response = await Context.RequestAsync("ollama-api", "generate",
            new { baseUrl, model, prompt });

        if (response.Status != ResponseStatus.Ok)
            throw new InvalidOperationException(
                $"Classifier.Rules[{index}] ASK failed: ollama {response.Status} {response.Text}");

        var generated = response.ResultAs<GenerateResult>()!.Response.Trim();
        var answer = UnwrapAnswer(generated);
        Context.Log(LogCategory.Debug, $"ask rule {index} response: {ForLog(answer)}");

        if (IsNoMatch(answer))
            return null;
        if (IsMatch(answer))
            return folder;

        Context.Log(LogCategory.Warning,
            $"ask rule {index} returned an unrecognized decision; treating as NO_MATCH: {ForLog(answer)}");
        return null;
    }

    private static (string Folder, string Remaining) ReadFolder(
        string arguments, int index, string command)
    {
        var trimmed = arguments.TrimStart();
        if (trimmed.Length == 0)
            throw new InvalidOperationException($"Classifier.Rules[{index}] {command} is malformed.");

        if (trimmed[0] != '"')
        {
            var separatorIndex = trimmed.IndexOfAny([' ', '\t', '\r', '\n']);
            if (separatorIndex < 1)
                throw new InvalidOperationException($"Classifier.Rules[{index}] {command} is malformed.");
            return (trimmed[..separatorIndex], RequireRemainder(trimmed[(separatorIndex + 1)..], index, command));
        }

        var escaped = false;
        for (var characterIndex = 1; characterIndex < trimmed.Length; characterIndex++)
        {
            var character = trimmed[characterIndex];
            if (character == '"' && !escaped)
            {
                var literal = trimmed[..(characterIndex + 1)];
                string? folder;
                try
                {
                    folder = JsonSerializer.Deserialize<string>(literal);
                }
                catch (JsonException exception)
                {
                    throw new InvalidOperationException(
                        $"Classifier.Rules[{index}] {command} has an invalid quoted folder name.", exception);
                }
                if (string.IsNullOrWhiteSpace(folder))
                    throw new InvalidOperationException($"Classifier.Rules[{index}] {command} is malformed.");
                return (folder, RequireRemainder(trimmed[(characterIndex + 1)..], index, command));
            }

            escaped = character == '\\' && !escaped;
            if (character != '\\')
                escaped = false;
        }

        throw new InvalidOperationException(
            $"Classifier.Rules[{index}] {command} has an unterminated quoted folder name.");
    }

    private static string RequireRemainder(string value, int index, string command)
    {
        var remaining = value.TrimStart();
        if (remaining.Length == 0)
            throw new InvalidOperationException($"Classifier.Rules[{index}] {command} is malformed.");
        return remaining;
    }

    private static string BuildAskPrompt(ClassifyInput message, string criterion)
    {
        var bodyPreview = TruncateUtf8(message.BodyText, 1000);
        return $"""
            Determine whether this email matches exactly one criterion.
            Return exactly MATCH only when the email contains clear evidence for the criterion.
            Return exactly NO_MATCH if the email does not match or if you are uncertain.
            Do not classify by topic unless that topic is present in the email.
            Do not include quotes, explanation, markdown, or punctuation.

            Criterion:
            {criterion}

            Email headers:
            From: {message.From}
            To: {message.To}
            Subject: {message.Subject}
            Date: {message.Date}

            First up to 1000 UTF-8 bytes of message body:
            {bodyPreview}
            """;
    }

    private static string TruncateUtf8(string text, int maxBytes)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length <= maxBytes)
            return text;

        var strictUtf8 = new UTF8Encoding(false, true);
        for (var length = maxBytes; length > 0; length--)
        {
            try
            {
                return strictUtf8.GetString(bytes, 0, length);
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return "";
    }

    private static string UnwrapAnswer(string answer)
    {
        if (answer.Length >= 2 &&
            ((answer[0] == '"' && answer[^1] == '"') ||
             (answer[0] == '\'' && answer[^1] == '\'') ||
             (answer[0] == '`' && answer[^1] == '`')))
        {
            return answer[1..^1].Trim();
        }
        return answer;
    }

    private static bool IsNoMatch(string answer)
    {
        var normalized = answer.Replace('-', '_').Replace(' ', '_');
        return normalized.Contains("NO_MATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatch(string answer) =>
        answer.Equals("MATCH", StringComparison.OrdinalIgnoreCase) ||
        answer.Equals("EXACTLY MATCH", StringComparison.OrdinalIgnoreCase);

    private static string ForLog(string answer)
    {
        var singleLine = answer.Replace("\r", " ").Replace("\n", " ").Trim();
        return singleLine.Length <= 200 ? singleLine : $"{singleLine[..200]}...";
    }

    private string ReadRequiredString(string key)
    {
        try
        {
            var value = Context.GetConfigItem<string>(key);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Configuration '{key}' is empty.");
            return value;
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidOperationException($"Configuration '{key}' is missing.");
        }
    }

    private static string GetHeaderValue(ClassifyInput message, string headerName) =>
        headerName.ToUpperInvariant() switch
        {
            "FROM" => message.From,
            "TO" => message.To,
            "SUBJECT" => message.Subject,
            "DATE" => message.Date,
            _ => throw new InvalidOperationException($"Unsupported classifier header '{headerName}'.")
        };
}
