using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.Speak;

[BoxImplementation("speak-main")]
public sealed class SpeakMainBox : Box
{
    public override async Task RunAsync()
    {
        while (!Context.IsCancelled)
        {
            var promptResponse = await Context.RequestAsync(
                "text-input-api",
                "prompt",
                new TextInputBox.PromptInput("Speak", "Enter text to speak:"));

            if (promptResponse.Status == ResponseStatus.Error &&
                promptResponse.Text == "input-cancelled")
            {
                Context.Shutdown();
                return;
            }

            promptResponse.ThrowIfException();
            var result = promptResponse.ResultAs<TextInputBox.PromptResult>()!;
            if (string.IsNullOrEmpty(result.Text))
            {
                Context.Shutdown();
                return;
            }

            var sayResponse = await Context.RequestAsync(
                "text-to-speech-api",
                "say-and-wait",
                new TextToSpeechBox.SayInput(result.Text));
            sayResponse.ThrowIfException();
        }
    }
}
