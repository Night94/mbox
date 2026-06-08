using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("worker")]
public sealed class WorkerBox : Box
{
    public sealed record AddInput(int A, int B);
    public sealed record AddResult(int Sum);
    public sealed record SlowAddInput(int A, int B, int DelayMs);
    public sealed record DivideInput(int A, int B);
    public sealed record DivideResult(double Quotient);

    [OperationHandler("worker-api", "add")]
    public Task<AddResult> Add(AddInput input) =>
        Task.FromResult(new AddResult(input.A + input.B));

    [OperationHandler("worker-api", "slow-add")]
    public async Task<AddResult> SlowAdd(SlowAddInput input)
    {
        await Task.Delay(input.DelayMs);
        return new AddResult(input.A + input.B);
    }

    [OperationHandler("worker-api", "divide")]
    public Task<DivideResult> Divide(DivideInput input)
    {
        if (input.B == 0)
            throw new OperationError("divide-by-zero");
        return Task.FromResult(new DivideResult((double)input.A / input.B));
    }
}
