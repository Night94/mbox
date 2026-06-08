using System.Globalization;
using System.Text;
using Mbox;
using Mbox.Boxes;

namespace Mbox.Apps.ImapTest;

[BoxImplementation("imap-test-main")]
public sealed class ImapTestMainBox : Box
{
    private const int MaxUnmatchedMessages = 300;

    public override async Task RunAsync()
    {
        var host = Context.GetConfigItem<string>("imap.host")!;
        var user = Context.GetConfigItem<string>("imap.user")!;
        var pwd = Context.GetConfigItem<string>("imap.pwd")!;
        var folder = "INBOX";
        var report = new StringBuilder();

        await Context.RequestAsync(
            "display-api",
            "show-window",
            new DisplayBox.ShowWindowInput(0, 60.0, 60.0, 20.0, 20.0));

        await Context.RequestAsync(
            "display-api",
            "show-string",
            new DisplayBox.TextInput($"Loading {folder}..."));

        var countResponse = await Context.RequestAsync(
            "imap-api",
            "count-messages",
            new ImapBox.CountMessagesInput(host, user, pwd, folder));
        countResponse.ThrowIfException();
        var totalCount = countResponse.ResultAs<ImapBox.CountMessagesResult>()!.Count;
        var remainingCount = totalCount;
        var unmatchedCount = 0;
        int? processingYear = null;

        report.AppendLine(
            $"Folder {folder}: {totalCount} message(s), processing until {MaxUnmatchedMessages} unmatched message(s) are found.");
        await ShowReportAsync(report);

        for (var index = 0;
             index < remainingCount && unmatchedCount < MaxUnmatchedMessages;)
        {
            if (Context.IsCancelled)
                break;

            var loadResponse = await Context.RequestAsync(
                "imap-api",
                "load-by-date-at",
                new ImapBox.LoadByDateAtInput(host, user, pwd, folder, index));
            if (loadResponse.Status != ResponseStatus.Ok)
            {
                report.AppendLine($"[load {index}] {loadResponse.Status}: {loadResponse.Text}");
                await ShowReportAsync(report);
                index++;
                continue;
            }
            var loaded = loadResponse.ResultAs<ImapBox.LoadResult>()!;
            var messageYear = DateTimeOffset.ParseExact(
                loaded.Date,
                "u",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal).Year;
            if (processingYear is null)
            {
                processingYear = messageYear;
                report.AppendLine($"Processing messages from calendar year {processingYear.Value} only.");
                await ShowReportAsync(report);
            }
            else if (messageYear != processingYear.Value)
            {
                report.AppendLine();
                report.AppendLine(
                    $"Reached a message dated in {messageYear}; stopped after calendar year {processingYear.Value}.");
                break;
            }

            var messageLine = $"{loaded.Date} {loaded.From} {loaded.To}";

            var classifyResponse = await Context.RequestAsync(
                "mail-classifier-api",
                "classify",
                new MailClassifierBox.ClassifyInput(
                    loaded.Folder, loaded.Uid, loaded.UidValidity,
                    loaded.From, loaded.To, loaded.Subject, loaded.Date, loaded.BodyText));

            if (classifyResponse.Status == ResponseStatus.Error &&
                classifyResponse.Text == "no-matching-rule")
            {
                report.AppendLine($"{messageLine} -> not moved");
                await ShowReportAsync(report);
                unmatchedCount++;
                index++;
                continue;
            }
            if (classifyResponse.Status != ResponseStatus.Ok)
            {
                report.AppendLine($"{messageLine} -> not moved");
                report.AppendLine($"[classify uid={loaded.Uid}] {classifyResponse.Status}: {classifyResponse.Text}");
                await ShowReportAsync(report);
                index++;
                continue;
            }
            var destination = classifyResponse.ResultAs<MailClassifierBox.ClassifyResult>()!.Folder;

            var moveResponse = await Context.RequestAsync(
                "imap-api",
                "move-message",
                new ImapBox.MoveMessageInput(
                    host, user, pwd,
                    loaded.Folder, loaded.Uid, loaded.UidValidity, destination));
            if (moveResponse.Status != ResponseStatus.Ok)
            {
                report.AppendLine($"{messageLine} -> not moved");
                report.AppendLine($"[move uid={loaded.Uid} -> {destination}] {moveResponse.Status}: {moveResponse.Text}");
                await ShowReportAsync(report);
                index++;
                continue;
            }
            report.AppendLine($"{messageLine} -> {destination}");
            await ShowReportAsync(report);
            // A moved message leaves the date-ordered INBOX view, so the next message is at this index.
            remainingCount--;
        }

        report.AppendLine();
        report.AppendLine($"Done. Found {unmatchedCount} unmatched message(s).");

        await ShowReportAsync(report);

        await Task.Delay(60_000);
        Context.Shutdown();
    }

    private async Task ShowReportAsync(StringBuilder report)
    {
        await Context.RequestAsync(
            "display-api",
            "use-multitext",
            new DisplayBox.TextInput(report.ToString()));
    }
}
