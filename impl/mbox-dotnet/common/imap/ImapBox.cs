using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Mbox;

namespace Mbox.Boxes;

[BoxImplementation("imap")]
public sealed class ImapBox : Box
{
    public sealed record TestConnectionInput(string Host, string User, string Pwd);
    public sealed record TestConnectionResult(bool Success, string Message);

    public sealed record CountMessagesInput(string Host, string User, string Pwd, string Folder);
    public sealed record CountMessagesResult(string Folder, int Count);

    public sealed record LoadOldestInput(string Host, string User, string Pwd, string Folder);
    public sealed record LoadAtInput(string Host, string User, string Pwd, string Folder, int Index);
    public sealed record LoadByDateAtInput(string Host, string User, string Pwd, string Folder, int Index);
    public sealed record LoadResult(
        string Folder, long Uid, long UidValidity,
        string From, string To, string Subject, string Date, string BodyText);

    public sealed record MoveMessageInput(
        string Host, string User, string Pwd,
        string SourceFolder, long Uid, long UidValidity, string DestinationFolder);
    public sealed record MoveMessageResult(
        string SourceFolder, string DestinationFolder, long Uid, bool DestinationCreated);

    [OperationHandler("imap-api", "test-connection")]
    public async Task<TestConnectionResult> TestConnection(TestConnectionInput input)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(input.Host, 993, SecureSocketOptions.SslOnConnect, cts.Token);
            await client.AuthenticateAsync(input.User, input.Pwd, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
            return new TestConnectionResult(true, "login succeeded");
        }
        catch (AuthenticationException exception)
        {
            return new TestConnectionResult(false, $"auth failed: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            return new TestConnectionResult(false, "timed out after 10s");
        }
        catch (Exception exception)
        {
            return new TestConnectionResult(false, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    [OperationHandler("imap-api", "count-messages")]
    public async Task<CountMessagesResult> CountMessages(CountMessagesInput input)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new ImapClient();
        await client.ConnectAsync(input.Host, 993, SecureSocketOptions.SslOnConnect, cts.Token);
        await client.AuthenticateAsync(input.User, input.Pwd, cts.Token);

        var folder = await ResolveFolderAsync(client, input.Folder, cts.Token);
        await folder.OpenAsync(FolderAccess.ReadOnly, cts.Token);
        var count = folder.Count;
        await folder.CloseAsync(false, cts.Token);
        await client.DisconnectAsync(true, cts.Token);

        return new CountMessagesResult(input.Folder, count);
    }

    [OperationHandler("imap-api", "load-oldest")]
    public Task<LoadResult> LoadOldest(LoadOldestInput input) =>
        LoadMessageAtAsync(input.Host, input.User, input.Pwd, input.Folder, 0, "folder-empty");

    [OperationHandler("imap-api", "load-at")]
    public Task<LoadResult> LoadAt(LoadAtInput input) =>
        LoadMessageAtAsync(input.Host, input.User, input.Pwd, input.Folder, input.Index, "message-index-out-of-range");

    [OperationHandler("imap-api", "load-by-date-at")]
    public Task<LoadResult> LoadByDateAt(LoadByDateAtInput input) =>
        LoadMessageByDateAtAsync(input.Host, input.User, input.Pwd, input.Folder, input.Index);

    [OperationHandler("imap-api", "move-message")]
    public async Task<MoveMessageResult> MoveMessage(MoveMessageInput input)
    {
        if (input.SourceFolder.Equals(input.DestinationFolder, StringComparison.OrdinalIgnoreCase))
            throw new OperationError("same-source-and-destination");

        if (input.Uid < 1 || input.Uid > uint.MaxValue)
            throw new OperationError("invalid-message-uid");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new ImapClient();
        await client.ConnectAsync(input.Host, 993, SecureSocketOptions.SslOnConnect, cts.Token);
        await client.AuthenticateAsync(input.User, input.Pwd, cts.Token);

        var source = await ResolveFolderAsync(client, input.SourceFolder, cts.Token);
        await source.OpenAsync(FolderAccess.ReadWrite, cts.Token);
        if ((long)source.UidValidity != input.UidValidity)
        {
            await source.CloseAsync(false, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
            throw new OperationError("stale-uid-validity");
        }

        IMailFolder? destination;
        var destinationCreated = false;
        try
        {
            destination = await client.GetFolderAsync(input.DestinationFolder, cts.Token);
        }
        catch (FolderNotFoundException)
        {
            destination = null;
        }

        if (destination is null)
        {
            var root = client.GetFolder(client.PersonalNamespaces[0]);
            var parent = root;
            var childName = input.DestinationFolder;
            var separatorIndex = input.DestinationFolder.LastIndexOf(root.DirectorySeparator);
            if (separatorIndex >= 0)
            {
                var parentName = input.DestinationFolder[..separatorIndex];
                childName = input.DestinationFolder[(separatorIndex + 1)..];
                parent = await ResolveFolderAsync(client, parentName, cts.Token);
            }

            destination = await parent.CreateAsync(childName, true, cts.Token)
                ?? throw new InvalidOperationException(
                    $"IMAP did not return the newly created destination folder '{input.DestinationFolder}'.");
            destinationCreated = true;
        }

        var uid = new UniqueId((uint)input.Uid);
        await source.MoveToAsync(new[] { uid }, destination, cts.Token);
        await source.CloseAsync(false, cts.Token);
        await client.DisconnectAsync(true, cts.Token);

        return new MoveMessageResult(input.SourceFolder, input.DestinationFolder, input.Uid, destinationCreated);
    }

    private static async Task<LoadResult> LoadMessageAtAsync(
        string host, string user, string pwd, string folderName, int index, string missingMessageError)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new ImapClient();
        await client.ConnectAsync(host, 993, SecureSocketOptions.SslOnConnect, cts.Token);
        await client.AuthenticateAsync(user, pwd, cts.Token);

        var folder = await ResolveFolderAsync(client, folderName, cts.Token);
        await folder.OpenAsync(FolderAccess.ReadOnly, cts.Token);
        if (index >= folder.Count)
        {
            await folder.CloseAsync(false, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
            throw new OperationError(missingMessageError);
        }

        var summaries = await folder.FetchAsync(index, index, MessageSummaryItems.UniqueId, cts.Token);
        var uid = summaries[0].UniqueId;
        var message = await folder.GetMessageAsync(index, cts.Token);
        var uidValidity = (long)folder.UidValidity;

        await folder.CloseAsync(false, cts.Token);
        await client.DisconnectAsync(true, cts.Token);

        return new LoadResult(
            Folder: folderName,
            Uid: uid.Id,
            UidValidity: uidValidity,
            From: message.From.ToString(),
            To: message.To.ToString(),
            Subject: message.Subject ?? "",
            Date: message.Date.ToString("u"),
            BodyText: message.TextBody ?? message.HtmlBody ?? "");
    }

    private static async Task<LoadResult> LoadMessageByDateAtAsync(
        string host, string user, string pwd, string folderName, int index)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new ImapClient();
        await client.ConnectAsync(host, 993, SecureSocketOptions.SslOnConnect, cts.Token);
        await client.AuthenticateAsync(user, pwd, cts.Token);

        var folder = await ResolveFolderAsync(client, folderName, cts.Token);
        await folder.OpenAsync(FolderAccess.ReadOnly, cts.Token);
        if (index < 0 || index >= folder.Count)
        {
            await folder.CloseAsync(false, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
            throw new OperationError("message-index-out-of-range");
        }

        var summaries = await folder.FetchAsync(
            0,
            -1,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope,
            cts.Token);
        var selected = summaries
            .OrderBy(summary => summary.Date)
            .ThenBy(summary => summary.UniqueId.Id)
            .ElementAt(index);
        var message = await folder.GetMessageAsync(selected.UniqueId, cts.Token);
        var uidValidity = (long)folder.UidValidity;

        await folder.CloseAsync(false, cts.Token);
        await client.DisconnectAsync(true, cts.Token);

        return new LoadResult(
            Folder: folderName,
            Uid: selected.UniqueId.Id,
            UidValidity: uidValidity,
            From: message.From.ToString(),
            To: message.To.ToString(),
            Subject: message.Subject ?? "",
            Date: message.Date.ToString("u"),
            BodyText: message.TextBody ?? message.HtmlBody ?? "");
    }

    private static async Task<IMailFolder> ResolveFolderAsync(
        ImapClient client, string folderName, CancellationToken cancellationToken)
    {
        try
        {
            var folder = folderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folderName, cancellationToken);
            if (folder is null)
            {
                await client.DisconnectAsync(true, cancellationToken);
                throw new OperationError("unknown-folder");
            }
            return folder;
        }
        catch (FolderNotFoundException)
        {
            await client.DisconnectAsync(true, cancellationToken);
            throw new OperationError("unknown-folder");
        }
    }
}
