using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.Bday;

[BoxImplementation("bday-main")]
public sealed class BdayMainBox : Box
{
    public override async Task RunAsync()
    {
        var baseUrl = Context.GetConfigItem<string>("ollama.baseUrl")!;
        var model = Context.GetConfigItem<string>("ollama.model")!;

        var promptTask = Context.RequestAsync(
            "text-input-api",
            "prompt",
            new TextInputBox.PromptInput("Birthday", "What's your birthday?"));
        var spokenPromptTask = Context.SendOnceAsync(
            "text-to-speech-api",
            "say",
            new TextToSpeechBox.SayInput("What's your birthday?"));

        await spokenPromptTask;
        var promptResponse = await promptTask;
        if (promptResponse.Status == ResponseStatus.Error &&
            promptResponse.Text == "input-cancelled")
        {
            Context.Shutdown();
            return;
        }

        promptResponse.ThrowIfException();
        var birthday = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text.Trim();
        if (string.IsNullOrEmpty(birthday))
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
                $"The user's birthday is {birthday}. Name three to five famous people who share that month and day. Reply as one concise, spoken-friendly paragraph without markdown."));

        string spokenAnswer;
        if (generated.Status == ResponseStatus.Ok)
        {
            spokenAnswer = generated.ResultAs<OllamaBox.GenerateResult>()!.Response;
        }
        else
        {
            Context.Log(LogCategory.Warning, $"birthday lookup failed: {generated.Status} {generated.Text}");
            spokenAnswer = "Sorry, I couldn't look up famous people with that birthday right now.";
        }

        var answerResponse = await Context.RequestAsync(
            "text-to-speech-api",
            "say-and-wait",
            new TextToSpeechBox.SayInput(spokenAnswer));
        answerResponse.ThrowIfException();
        Context.Shutdown();
    }
}
