using System.Diagnostics;
using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.WorkerDemo;

[BoxImplementation("worker-demo-main")]
public sealed class WorkerDemoMainBox : Box
{
    public override async Task RunAsync()
    {
        var first = await Context.CreateBoxAsync("worker");
        var second = await Context.CreateBoxAsync("worker");
        Context.Log(LogCategory.Normal, $"created worker|{first} and worker|{second}");

        var timer = Stopwatch.StartNew();
        var operations = new[]
        {
            Context.RequestAsync("worker-api", "slow-add", new { a = 1, b = 2, delayMs = 800 }, first),
            Context.RequestAsync("worker-api", "slow-add", new { a = 3, b = 4, delayMs = 800 }, first),
            Context.RequestAsync("worker-api", "slow-add", new { a = 5, b = 6, delayMs = 800 }, first)
        };
        var results = await Task.WhenAll(operations);
        timer.Stop();
        var sums = results.Select(result => result.ResultAs<WorkerBox.AddResult>()!.Sum);
        Context.Log(LogCategory.Normal, $"parallel slow-add took {timer.ElapsedMilliseconds}ms; sums={string.Join(", ", sums)}");

        try
        {
            await Context.RequestAsync(
                "worker-api",
                "slow-add",
                new { a = 1, b = 1, delayMs = 3000 },
                first,
                timeoutMs: 500);
            throw new InvalidOperationException("Expected slow-add request to time out.");
        }
        catch (TimeoutException exception)
        {
            Context.Log(LogCategory.Normal, $"timeout observed: {exception.Message}");
        }

        var division = await Context.RequestAsync("worker-api", "divide", new { a = 10, b = 0 }, first);
        if (division.Status != ResponseStatus.Error || division.Text != "divide-by-zero")
            throw new InvalidOperationException("Expected the divide-by-zero declared failure.");
        Context.Log(LogCategory.Normal, "declared divide-by-zero failure observed");

        var addition = await Context.RequestAsync("worker-api", "add", new { a = 2, b = 3 }, second);
        var sum = addition.ResultAs<WorkerBox.AddResult>()?.Sum;
        if (addition.Status != ResponseStatus.Ok || sum != 5)
            throw new InvalidOperationException("Expected worker-api.add to return sum 5.");
        Context.Log(LogCategory.Normal, "worker-api.add returned sum=5");

        await Context.DestroyAsync($"worker|{first}");
        await Context.DestroyAsync($"worker|{second}");
        Context.Shutdown();
    }

    public override Task DeinitAsync()
    {
        Context.Log(LogCategory.Normal, "entry box shutting down");
        return Task.CompletedTask;
    }
}
