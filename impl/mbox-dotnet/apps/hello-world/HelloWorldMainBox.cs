using Mbox;

namespace Mbox.Apps.HelloWorld;

[BoxImplementation("hello-world-main")]
public sealed class HelloWorldMainBox : Box
{
    public override async Task RunAsync()
    {
        await Context.RequestAsync(
            "display-api",
            "show-window",
            new { monitorId = 0, width = 50.0, height = 50.0, left = 25.0, top = 25.0 });
        await Task.Delay(5000);
        await Context.RequestAsync("display-api", "show-string", new { text = "hello world" });
        await Task.Delay(5000);
        Context.Shutdown();
    }
}
