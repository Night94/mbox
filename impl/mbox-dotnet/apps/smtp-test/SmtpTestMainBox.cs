using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.SmtpTest;

[BoxImplementation("smtp-test-main")]
public sealed class SmtpTestMainBox : Box
{
    public override async Task RunAsync()
    {
        var host = Context.GetConfigItem<string>("smtp.host")!;
        var port = Context.GetConfigItem<int>("smtp.port");
        var startTls = Context.GetConfigItem<bool>("smtp.startTls");
        var user = Context.GetConfigItem<string>("smtp.user")!;
        var pwd = Context.GetConfigItem<string>("smtp.pwd")!;
        var from = Context.GetConfigItem<string>("smtp.from")!;
        var to = Context.GetConfigItem<string>("smtp.to")!;

        var promptResponse = await Context.RequestAsync(
            "text-input-api",
            "prompt",
            new TextInputBox.PromptInput(
                "SMTP test",
                "Enter text to send as the email subject and body:"));

        if (promptResponse.Status == ResponseStatus.Error &&
            promptResponse.Text == "input-cancelled")
        {
            Context.Shutdown();
            return;
        }

        promptResponse.ThrowIfException();
        var text = promptResponse.ResultAs<TextInputBox.PromptResult>()!.Text;
        var sendResponse = await Context.RequestAsync(
            "smtp-api",
            "send",
            new SmtpBox.SendInput(host, port, startTls, user, pwd, from, to, text, text));
        sendResponse.ThrowIfException();
        var sent = sendResponse.ResultAs<SmtpBox.SendResult>()!;
        Context.Log(LogCategory.Normal, $"SMTP message sent successfully: {sent.ServerResponse}");
        Context.Shutdown();
    }
}
