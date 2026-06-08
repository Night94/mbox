using System.Drawing;
using System.Windows.Forms;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("display")]
public sealed class DisplayBox : Box
{
    public sealed record ShowWindowInput(int MonitorId, double Width, double Height, double Left, double Top);
    public sealed record TextInput(string Text);
    public sealed record EmptyInput();

    private readonly TaskCompletionSource _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread? _uiThread;
    private Form? _form;
    private bool _closing;

    public override Task InitAsync()
    {
        _uiThread = new Thread(RunUi) { IsBackground = true };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        return _ready.Task;
    }

    [OperationHandler("display-api", "show-window")]
    public Task ShowWindow(ShowWindowInput input)
    {
        var screens = Screen.AllScreens;
        if (input.MonitorId < 0 || input.MonitorId >= screens.Length)
            throw new OperationError("unknown-monitor");
        var area = screens[input.MonitorId].WorkingArea;
        var width = (int)(area.Width * input.Width / 100.0);
        var height = (int)(area.Height * input.Height / 100.0);
        var left = area.Left + (int)(area.Width * input.Left / 100.0);
        var top = area.Top + (int)(area.Height * input.Top / 100.0);
        _form!.Invoke(() =>
        {
            _form.Bounds = new Rectangle(left, top, Math.Max(50, width), Math.Max(50, height));
            if (!_form.Visible)
                _form.Show();
            _form.BringToFront();
        });
        return Task.CompletedTask;
    }

    [OperationHandler("display-api", "hide-window")]
    public Task HideWindow(EmptyInput _)
    {
        _form!.Invoke(() => _form.Hide());
        return Task.CompletedTask;
    }

    [OperationHandler("display-api", "show-string")]
    public Task ShowString(TextInput input)
    {
        _form!.Invoke(() =>
        {
            _form.Controls.Clear();
            _form.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24f, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.Black,
                Text = input.Text
            });
        });
        return Task.CompletedTask;
    }

    [OperationHandler("display-api", "use-multitext")]
    public Task UseMultitext(TextInput input)
    {
        _form!.Invoke(() =>
        {
            _form.Controls.Clear();
            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                Font = new Font("Consolas", 11f),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Text = input.Text
            };
            _form.Controls.Add(textBox);
            textBox.SelectionStart = textBox.TextLength;
            textBox.ScrollToCaret();
        });
        return Task.CompletedTask;
    }

    public override Task DeinitAsync()
    {
        if (_form is not null && !_form.IsDisposed)
        {
            _form.Invoke(() =>
            {
                _closing = true;
                _form.Close();
                Application.ExitThread();
            });
        }
        _uiThread?.Join(2000);
        return Task.CompletedTask;
    }

    private void RunUi()
    {
        _form = new Form
        {
            Text = "mbox display",
            StartPosition = FormStartPosition.Manual,
            Width = 400,
            Height = 200,
            BackColor = Color.Black,
            ForeColor = Color.White,
            FormBorderStyle = FormBorderStyle.Sizable,
            ShowInTaskbar = true
        };
        _form.HandleCreated += (_, _) => _ready.TrySetResult();
        _form.FormClosing += (_, eventArgs) =>
        {
            if (!_closing)
            {
                eventArgs.Cancel = true;
                _form.Hide();
            }
        };
        _ = _form.Handle;
        Application.Run();
    }
}
