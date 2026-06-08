using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.NameMeaning;

[BoxImplementation("name-meaning-main")]
public sealed class NameMeaningMainBox : Box
{
    public override async Task RunAsync()
    {
        var baseUrl = Context.GetConfigItem<string>("ollama.baseUrl")!;
        var model = Context.GetConfigItem<string>("ollama.model")!;

        const string question = "What is your first and last name?";
        var promptTask = Context.RequestAsync(
            "text-input-api",
            "prompt",
            new TextInputBox.PromptInput("Name Meaning", question));
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
        var fullName = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text.Trim();
        if (string.IsNullOrEmpty(fullName))
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
                $"The user supplied this full name: {fullName}. Explain the likely linguistic or cultural origins and meanings of the first name and the last name separately. Be cautious where origins are uncertain or shared across cultures. Reply as one warm, spoken-friendly paragraph of fewer than 90 words, without markdown or headings.",
                Temperature: 0.2));

        string spokenAnswer;
        if (generated.Status == ResponseStatus.Ok)
        {
            var answer = generated.ResultAs<OllamaBox.GenerateResult>()!.Response;
            spokenAnswer = LimitToNinetyNineWords(answer);
        }
        else
        {
            Context.Log(LogCategory.Warning, $"name lookup failed: {generated.Status} {generated.Text}");
            spokenAnswer = "Sorry, I couldn't look up the origins of your name right now.";
        }

        var answerResponse = await Context.RequestAsync(
            "text-to-speech-api",
            "say-and-wait",
            new TextToSpeechBox.SayInput(spokenAnswer));
        answerResponse.ThrowIfException();
        Context.Shutdown();
    }

    private static string LimitToNinetyNineWords(string answer)
    {
        var words = answer.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "I couldn't find a clear explanation for that name.";

        if (words.Length < 100)
            return string.Join(" ", words);

        var shortened = string.Join(" ", words.Take(99)).TrimEnd(',', ';', ':');
        return shortened.EndsWith('.') ? shortened : $"{shortened}.";
    }
}
