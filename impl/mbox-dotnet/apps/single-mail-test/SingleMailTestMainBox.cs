using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.SingleMailTest;

[BoxImplementation("single-mail-test-main")]
public sealed class SingleMailTestMainBox : Box
{
    private const int MaxMessages = 10;
    private const string InitialPrompt =
        "You are a mail classifier. Read the email, consider the headers and body. Understand the sentiment and purpose of it. Answer with JUST ONE NUMBER: confidency level that this is an advertisement. Number must be in range 0..100";
    private const string StructuredAnswerInstruction =
        "Return only a JSON object matching this shape: {\"advertisementConfidencePercent\": <integer from 0 through 100>, \"reason\": <short text explaining the reason (less than 100 chars)>}.";
    private static readonly JsonNode ConfidenceFormat = JsonNode.Parse(
        """
        {
          "type": "object",
          "properties": {
        "advertisementConfidencePercent": {
          "type": "integer",
          "minimum": 0,
          "maximum": 100
        },
        "reason": {
          "type": "string"
        }
          },
          "required": ["advertisementConfidencePercent", "reason"],
          "additionalProperties": false
        }
        """)!;

    private sealed record ConfidenceResult(int AdvertisementConfidencePercent);

    public override async Task RunAsync()
    {
        var host = Context.GetConfigItem<string>("imap.host")!;
        var user = Context.GetConfigItem<string>("imap.user")!;
        var pwd = Context.GetConfigItem<string>("imap.pwd")!;
        var baseUrl = Context.GetConfigItem<string>("ollama.baseUrl")!;
        var model = Context.GetConfigItem<string>("ollama.model")!;

        await Context.RequestAsync(
            "display-api",
            "show-window",
            new DisplayBox.ShowWindowInput(0, 60.0, 80.0, 2.0, 10.0));
        await Context.RequestAsync(
            "display-api",
            "show-string",
            new DisplayBox.TextInput($"Loading up to {MaxMessages} earliest-dated emails from INBOX..."));

        var emailTexts = new List<string>();
        for (var index = 0; index < MaxMessages; index++)
        {
            var loadResponse = await Context.RequestAsync(
                "imap-api",
                "load-by-date-at",
                new ImapBox.LoadByDateAtInput(host, user, pwd, "INBOX", index));
            loadResponse.ThrowIfException();
            if (loadResponse.Status == ResponseStatus.Error &&
                loadResponse.Text == "message-index-out-of-range")
            {
                break;
            }
            if (loadResponse.Status != ResponseStatus.Ok)
            {
                await Context.RequestAsync(
                    "display-api",
                    "use-multitext",
                    new DisplayBox.TextInput(
                        $"Unable to load email {index + 1} from INBOX: {loadResponse.Text}"));
                Context.Shutdown();
                return;
            }

            emailTexts.Add(FormatEmail(index + 1, loadResponse.ResultAs<ImapBox.LoadResult>()!));
        }
        if (emailTexts.Count == 0)
        {
            await Context.RequestAsync(
                "display-api",
                "use-multitext",
                new DisplayBox.TextInput("INBOX contains no emails to classify."));
            Context.Shutdown();
            return;
        }

        await Context.RequestAsync(
            "display-api",
            "use-multitext",
            new DisplayBox.TextInput(string.Join("\n\n========================================\n\n", emailTexts)));

        var prompt = InitialPrompt;
        var trial = 0;
        while (!Context.IsCancelled)
        {
            var promptResponse = await Context.RequestAsync(
                "text-input-api",
                "prompt",
                new TextInputBox.PromptInput(
                    "Single mail Ollama test",
                    "Edit the instruction to send with the displayed email:",
                    prompt,
                    true));

            if (promptResponse.Status == ResponseStatus.Error &&
                promptResponse.Text == "input-cancelled")
            {
                Context.Shutdown();
                return;
            }

            promptResponse.ThrowIfException();
            prompt = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text;
            trial++;
            for (var index = 0; index < emailTexts.Count; index++)
            {
                var generated = await Context.RequestAsync(
                    "ollama-api",
                    "generate",
                    new OllamaBox.GenerateInput(
                        baseUrl,
                        model,
                        ComposeRequest(prompt, emailTexts[index]),
                        ConfidenceFormat,
                        0));

                if (generated.Status == ResponseStatus.Ok)
                {
                    var answer = generated.ResultAs<OllamaBox.GenerateResult>()!.Response;
                    LogStructuredResponse(trial, index + 1, prompt, answer);
                }
                else
                {
                    Context.Log(
                        LogCategory.Warning,
                        $"Trial {trial}, email {index + 1} of {emailTexts.Count} instruction:\n{prompt}\n\nOllama generation failed: {generated.Status} {generated.Text}");
                }
            }
        }
    }

    private void LogStructuredResponse(int trial, int emailNumber, string prompt, string answer)
    {
        try
        {
            var confidence = JsonSerializer.Deserialize<ConfidenceResult>(
                answer,
                Framework.JsonOptions);
            if (confidence is null ||
                confidence.AdvertisementConfidencePercent is < 0 or > 100)
            {
                throw new JsonException("The confidence is absent or outside the range 0 through 100.");
            }

            Context.Log(
                LogCategory.Normal,
                $"Trial {trial}, email {emailNumber} instruction:\n{prompt}\n\nAdvertisement confidence: {confidence.AdvertisementConfidencePercent}%\nRaw JSON: {answer}");
        }
        catch (JsonException exception)
        {
            Context.Log(
                LogCategory.Warning,
                $"Trial {trial}, email {emailNumber} instruction:\n{prompt}\n\nInvalid structured Ollama response: {exception.Message}\nRaw response: {answer}");
        }
    }

    private static string FormatEmail(int emailNumber, ImapBox.LoadResult email)
    {
        var text = new StringBuilder();
        text.AppendLine($"Email {emailNumber} of up to {MaxMessages}, oldest-dated first");
        text.AppendLine();
        text.AppendLine($"From: {email.From}");
        text.AppendLine($"To: {email.To}");
        text.AppendLine($"Subject: {email.Subject}");
        text.AppendLine($"Date: {email.Date}");
        text.AppendLine();
        text.AppendLine("Body:");
        text.Append(email.BodyText);
        return text.ToString();
    }

    private static string ComposeRequest(string prompt, string emailText) =>
        $"{prompt}\n\n{StructuredAnswerInstruction}\n\n--- Email headers and body (attachments omitted) ---\n{emailText}";
}
