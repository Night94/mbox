using System.Runtime.InteropServices;
using Mbox;
using SherpaOnnx;

namespace Mbox.Boxes;

[BoxImplementation("text-to-speech")]
public sealed class TextToSpeechBox : Box
{
    public sealed record SayInput(string Text);

    private const uint SoundSync = 0x00000000;
    private const uint SoundNoDefault = 0x00000002;
    private const uint SoundFilename = 0x00020000;

    private readonly SemaphoreSlim _speechLock = new(1, 1);
    private OfflineTts? _tts;
    private int _speakerId = 1;
    private float _speed = 1.0f;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string path, IntPtr module, uint flags);

    public override Task InitAsync()
    {
        var modelDir = FindModelDirectory();
        _speakerId = GetOptionalInt("tts.speakerId", 1);
        _speed = (float)GetOptionalDouble("tts.speed", 1.0);

        var config = new OfflineTtsConfig();
        config.Model.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
        config.Model.Provider = "cpu";
        config.Model.Kokoro.Model = Path.Combine(modelDir, "model.onnx");
        config.Model.Kokoro.Voices = Path.Combine(modelDir, "voices.bin");
        config.Model.Kokoro.Tokens = Path.Combine(modelDir, "tokens.txt");
        config.Model.Kokoro.DataDir = Path.Combine(modelDir, "espeak-ng-data");
        config.Model.Kokoro.LengthScale = 1.0f;
        config.MaxNumSentences = 1;

        _tts = new OfflineTts(config);
        if (_speakerId < 0 || _speakerId >= _tts.NumSpeakers)
        {
            throw new InvalidOperationException(
                $"tts.speakerId {_speakerId} is outside the supported range 0-{_tts.NumSpeakers - 1}.");
        }

        Context.Log(LogCategory.Normal,
            $"Kokoro text-to-speech ready: speakerId={_speakerId}, speed={_speed:0.##}");
        return Task.CompletedTask;
    }

    [OperationHandler("text-to-speech-api", "say")]
    public Task Say(SayInput input) => Speak(input.Text);

    [OperationHandler("text-to-speech-api", "say-and-wait")]
    public Task SayAndWait(SayInput input) => Speak(input.Text);

    private async Task Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        await _speechLock.WaitAsync();
        try
        {
            var tts = _tts ?? throw new InvalidOperationException("Kokoro has not been initialized.");
            var audio = tts.Generate(text, _speed, _speakerId);
            try
            {
                var wavPath = Path.Combine(Path.GetTempPath(), $"mbox-tts-{Guid.NewGuid():N}.wav");
                try
                {
                    if (!audio.SaveToWaveFile(wavPath))
                        throw new InvalidOperationException("Kokoro failed to create audio.");
                    if (!PlaySound(wavPath, IntPtr.Zero, SoundFilename | SoundSync | SoundNoDefault))
                    {
                        throw new InvalidOperationException(
                            $"Windows failed to play generated audio (error {Marshal.GetLastWin32Error()}).");
                    }
                }
                finally
                {
                    if (File.Exists(wavPath))
                        File.Delete(wavPath);
                }
            }
            finally
            {
                audio.Dispose();
            }
        }
        finally
        {
            _speechLock.Release();
        }
    }

    public override Task DeinitAsync()
    {
        _tts?.Dispose();
        _tts = null;
        _speechLock.Dispose();
        return Task.CompletedTask;
    }

    private static string FindModelDirectory()
    {
        var relativeCandidates = new[]
        {
            Path.Combine("impl", "mbox-dotnet", "common", "text-to-speech", "models", "kokoro-en-v0_19"),
            Path.Combine("common", "text-to-speech", "models", "kokoro-en-v0_19"),
        };
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                foreach (var relative in relativeCandidates)
                {
                    var candidate = Path.Combine(current.FullName, relative);
                    if (File.Exists(Path.Combine(candidate, "model.onnx")))
                        return candidate;
                }
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Kokoro model assets were not found under impl/mbox-dotnet/common/text-to-speech/models/kokoro-en-v0_19.");
    }

    private int GetOptionalInt(string key, int fallback)
    {
        try { return Context.GetConfigItem<int>(key); }
        catch (KeyNotFoundException) { return fallback; }
    }

    private double GetOptionalDouble(string key, double fallback)
    {
        try { return Context.GetConfigItem<double>(key); }
        catch (KeyNotFoundException) { return fallback; }
    }
}
