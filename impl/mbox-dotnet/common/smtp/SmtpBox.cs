using System.Diagnostics;
using System.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("smtp")]
public sealed class SmtpBox : Box
{
    public sealed record SendInput(
        string Host,
        int Port,
        bool StartTls,
        string User,
        string Pwd,
        string From,
        string To,
        string Subject,
        string BodyText);

    public sealed record SendResult(string ServerResponse);

    [OperationHandler("smtp-api", "send")]
    public async Task<SendResult> Send(SendInput input)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var timer = Stopwatch.StartNew();
        var phase = "message preparation";
        var secureSocketOptions = input.StartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        using var protocolStream = new MemoryStream();
        using var protocolLogger = new ProtocolLogger(protocolStream, true)
        {
            RedactSecrets = true,
            LogTimestamps = true
        };
        using var client = new SmtpClient(protocolLogger);

        Context.Log(
            LogCategory.Debug,
            $"SMTP send requested: endpoint={input.Host}:{input.Port}; secureSocketOptions={secureSocketOptions}; " +
            $"user={input.User}; password=<redacted>; from={input.From}; to={input.To}; " +
            $"subjectLength={input.Subject.Length}; bodyLength={input.BodyText.Length}; timeout=30s");

        try
        {
            Context.Log(LogCategory.Debug, "SMTP message preparation starting");
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(input.From));
            message.To.Add(MailboxAddress.Parse(input.To));
            message.Subject = input.Subject;
            message.Body = new TextPart("plain") { Text = input.BodyText };
            Context.Log(LogCategory.Debug, "SMTP message preparation complete");

            phase = "connect/STARTTLS";
            Context.Log(LogCategory.Debug, $"SMTP {phase} starting");
            await client.ConnectAsync(input.Host, input.Port, secureSocketOptions, cts.Token);
            var securityDetails = client.IsSecure
                ? $"sslProtocol={client.SslProtocol}; cipherSuite={client.SslCipherSuite}; " +
                  $"cipher={client.SslCipherAlgorithm}/{client.SslCipherStrength}; " +
                  $"hash={client.SslHashAlgorithm}/{client.SslHashStrength}; " +
                  $"keyExchange={client.SslKeyExchangeAlgorithm}/{client.SslKeyExchangeStrength}; "
                : "sslProtocol=<not-secure>; cipherSuite=<not-secure>; ";
            Context.Log(
                LogCategory.Debug,
                $"SMTP connected: isSecure={client.IsSecure}; isConnected={client.IsConnected}; " +
                securityDetails +
                $"capabilities={client.Capabilities}; " +
                $"authenticationMechanisms=[{string.Join(", ", client.AuthenticationMechanisms.Order())}]");

            phase = "authentication";
            Context.Log(LogCategory.Debug, $"SMTP {phase} starting: user={input.User}; password=<redacted>");
            await client.AuthenticateAsync(input.User, input.Pwd, cts.Token);
            Context.Log(LogCategory.Debug, $"SMTP authenticated: isAuthenticated={client.IsAuthenticated}");

            phase = "message submission";
            Context.Log(LogCategory.Debug, "SMTP message submission starting");
            var serverResponse = await client.SendAsync(message, cts.Token);
            Context.Log(
                LogCategory.Debug,
                $"SMTP message submission accepted after {timer.ElapsedMilliseconds}ms: {serverResponse}");

            phase = "disconnect";
            Context.Log(LogCategory.Debug, "SMTP disconnect starting");
            await client.DisconnectAsync(true, cts.Token);
            Context.Log(LogCategory.Debug, $"SMTP disconnected: isConnected={client.IsConnected}");

            return new SendResult(serverResponse);
        }
        catch (Exception exception)
        {
            Context.Log(
                LogCategory.Error,
                SanitizeForLog(
                    $"SMTP failure during {phase} after {timer.ElapsedMilliseconds}ms: {exception}",
                    input));
            throw;
        }
        finally
        {
            var transcript = Encoding.UTF8.GetString(protocolStream.ToArray()).TrimEnd();
            Context.Log(
                LogCategory.Debug,
                $"SMTP protocol transcript (MailKit authentication secrets redacted):{Environment.NewLine}" +
                SanitizeForLog(
                    string.IsNullOrWhiteSpace(transcript) ? "<no protocol traffic captured>" : transcript,
                    input));
        }
    }

    private static string SanitizeForLog(string text, SendInput input)
    {
        if (string.IsNullOrEmpty(input.Pwd))
            return text;

        var passwordBytes = Encoding.UTF8.GetBytes(input.Pwd);
        var plainAuthBytes = Encoding.UTF8.GetBytes($"\0{input.User}\0{input.Pwd}");
        return text
            .Replace(input.Pwd, "<redacted>", StringComparison.Ordinal)
            .Replace(Convert.ToBase64String(passwordBytes), "<redacted>", StringComparison.Ordinal)
            .Replace(Convert.ToBase64String(plainAuthBytes), "<redacted>", StringComparison.Ordinal);
    }
}
