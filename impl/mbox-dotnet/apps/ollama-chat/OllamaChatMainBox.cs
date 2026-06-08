using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.OllamaChat;

[BoxImplementation("ollama-chat-main")]
public sealed class OllamaChatMainBox : Box
{
    public override async Task RunAsync()
    {
        var baseUrl = Context.GetConfigItem<string>("ollama.baseUrl")!;
        var model = Context.GetConfigItem<string>("ollama.model")!;

        await Context.RequestAsync(
            "display-api",
            "show-window",
            new DisplayBox.ShowWindowInput(0, 50.0, 50.0, 25.0, 25.0));

        while (!Context.IsCancelled)
        {
            await Context.RequestAsync(
                "display-api",
                "show-string",
                new DisplayBox.TextInput("Waiting for input..."));

            var promptResponse = await Context.RequestAsync(
                "text-input-api",
                "prompt",
                new TextInputBox.PromptInput("Ollama chat", "Ask Ollama:"));

            if (promptResponse.Status == ResponseStatus.Error &&
                promptResponse.Text == "input-cancelled")
            {
                Context.Shutdown();
                return;
            }
            promptResponse.ThrowIfException();
            var entered = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text;
            if (string.IsNullOrEmpty(entered))
            {
                Context.Shutdown();
                return;
            }

            await Context.RequestAsync(
                "display-api",
                "show-string",
                new DisplayBox.TextInput("Generating..."));

            var generated = await Context.RequestAsync(
                "ollama-api",
                "generate",
                new OllamaBox.GenerateInput(baseUrl, model, entered));

            string body;
            if (generated.Status == ResponseStatus.Ok)
            {
                body = generated.ResultAs<OllamaBox.GenerateResult>()!.Response;
            }
            else
            {
                body = $"[{generated.Status}] {generated.Text}";
            }

            await Context.RequestAsync(
                "display-api",
                "use-multitext",
                new DisplayBox.TextInput(body));

            if (generated.Status == ResponseStatus.Ok)
            {
                var sayResponse = await Context.RequestAsync(
                    "text-to-speech-api",
                    "say-and-wait",
                    new TextToSpeechBox.SayInput(body));
                sayResponse.ThrowIfException();
            }
        }
    }
}
