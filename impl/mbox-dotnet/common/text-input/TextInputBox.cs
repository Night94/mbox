using System.Drawing;
using System.Windows.Forms;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("text-input")]
public sealed class TextInputBox : Box
{
    public sealed record PromptInput(
        string Title,
        string Prompt,
        string InitialText = "",
        bool Multiline = false);
    public sealed record PromptResult(string Text);

    [OperationHandler("text-input-api", "prompt")]
    public Task<PromptResult> Prompt(PromptInput input)
    {
        var completion = new TaskCompletionSource<PromptResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                using var form = new Form
                {
                    Text = input.Title,
                    ClientSize = input.Multiline ? new Size(700, 510) : new Size(420, 118),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = true
                };
                using var label = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 14),
                    Text = input.Prompt
                };
                using var textBox = new TextBox
                {
                    Location = new Point(12, 39),
                    Size = input.Multiline ? new Size(676, 414) : new Size(396, 23),
                    Text = input.InitialText,
                    Multiline = input.Multiline,
                    AcceptsReturn = input.Multiline,
                    AcceptsTab = input.Multiline,
                    ScrollBars = input.Multiline ? ScrollBars.Vertical : ScrollBars.None,
                    WordWrap = true
                };
                using var ok = new Button
                {
                    DialogResult = DialogResult.OK,
                    Location = input.Multiline ? new Point(532, 469) : new Point(252, 78),
                    Size = new Size(75, 28),
                    Text = "OK"
                };
                using var cancel = new Button
                {
                    DialogResult = DialogResult.Cancel,
                    Location = input.Multiline ? new Point(613, 469) : new Point(333, 78),
                    Size = new Size(75, 28),
                    Text = "Cancel"
                };

                form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                form.Shown += (_, _) =>
                {
                    textBox.Focus();
                    textBox.SelectionStart = textBox.TextLength;
                };

                if (form.ShowDialog() == DialogResult.OK)
                    completion.TrySetResult(new PromptResult(textBox.Text));
                else
                    completion.TrySetException(new OperationError("input-cancelled"));
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }
}
