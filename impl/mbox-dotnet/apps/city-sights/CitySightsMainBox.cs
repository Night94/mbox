using System.Text.RegularExpressions;
using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.CitySights;

[BoxImplementation("city-sights-main")]
public sealed class CitySightsMainBox : Box
{
    public override async Task RunAsync()
    {
        var baseUrl = Context.GetConfigItem<string>("ollama.baseUrl")!;
        var model = Context.GetConfigItem<string>("ollama.model")!;

        const string question = "Which city would you like to hear sightseeing sites for?";
        var promptTask = Context.RequestAsync(
            "text-input-api",
            "prompt",
            new TextInputBox.PromptInput("City Sights", question));
        var spokenPromptTask = Context.SendOnceAsync(
            "text-to-speech-api",
            "say",
            new TextToSpeechBox.SayInput(question));

        await spokenPromptTask;
        var promptResponse = await promptTask;
        if (promptResponse.Status == ResponseStatus.Error &&
            promptResponse.Text == "input-cancelled")
        {
            Context.Shutdown();
            return;
        }

        promptResponse.ThrowIfException();
        var city = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text.Trim();
        if (string.IsNullOrEmpty(city))
        {
            Context.Shutdown();
            return;
        }

        var generated = await Context.RequestAsync(
            "ollama-api",
            "generate",
            new OllamaBox.GenerateInput(
                baseUrl,
                model,
                $"List the five most important sightseeing sites of {city}. Reply with exactly five lines. Each line contains only the site's name, prefixed by its position and a period, like \"1. Name\". No descriptions, no extra text, no markdown.",
                Temperature: 0.2));

        string spokenAnswer;
        if (generated.Status == ResponseStatus.Ok)
        {
            var answer = generated.ResultAs<OllamaBox.GenerateResult>()!.Response;
            var names = ExtractSiteNames(answer);
            spokenAnswer = names.Count > 0
                ? string.Join(". ", names) + "."
                : "Sorry, I couldn't find sightseeing sites for that city.";
        }
        else
        {
            Context.Log(LogCategory.Warning, $"sights lookup failed: {generated.Status} {generated.Text}");
            spokenAnswer = "Sorry, I couldn't look up sightseeing sites right now.";
        }

        var answerResponse = await Context.RequestAsync(
            "text-to-speech-api",
            "say-and-wait",
            new TextToSpeechBox.SayInput(spokenAnswer));
        answerResponse.ThrowIfException();
        Context.Shutdown();
    }

    private static List<string> ExtractSiteNames(string answer)
    {
        var names = new List<string>();
        foreach (var raw in answer.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var match = Regex.Match(line, @"^\s*(?:\d+[\.\)]|[-*])\s*(.+?)\s*$");
            var name = match.Success ? match.Groups[1].Value : line;

            var colon = name.IndexOf(':');
            if (colon > 0 && colon < name.Length - 1)
                name = name[..colon].Trim();

            name = name.Trim('*', '_', '"', '\'', ' ', '.', '-');
            if (name.Length == 0)
                continue;

            names.Add(name);
            if (names.Count == 5)
                break;
        }
        return names;
    }
}
