using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using Mbox;

namespace Mbox.Apps.AutosortRefresh;

[BoxImplementation("autosort-refresh-main")]
public sealed class AutosortRefreshMainBox : Box
{
    private sealed record SourceConfiguration(string Host, string User, string Password);
    private sealed record Sample(
        uint Uid,
        string Subject,
        string From,
        string[] FromAddresses,
        string? Sender,
        string? ReplyTo,
        string? ListId);
    private sealed record Category(
        string DestinationFolder,
        string SampleFolder,
        int SampleCount,
        Sample[] Samples);
    private sealed record Rule(
        string DestinationFolder,
        string Header,
        string Value,
        int SampleCount,
        string Directive);
    private sealed record AddressCandidate(
        string DestinationFolder,
        string Address,
        string Domain,
        int SampleCount);
    private sealed record Report(
        string RootFolder,
        string SourceApplicationConfig,
        Category[] Categories,
        Rule[] Rules,
        string[] Issues);

    public override async Task RunAsync()
    {
        var sourceConfigPath = Context.GetConfigItem<string>("autosort.sourceApplicationConfig")!;
        var rootFolderName = Context.GetConfigItem<string>("autosort.rootFolder")!;
        var sourceConfig = await ReadSourceConfigurationAsync(sourceConfigPath);

        using var client = new ImapClient();
        await client.ConnectAsync(sourceConfig.Host, 993, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(sourceConfig.User, sourceConfig.Password);

        var rootFolder = await client.GetFolderAsync(rootFolderName);
        var categoryFolders = await rootFolder.GetSubfoldersAsync(false);
        var categories = new List<Category>();
        foreach (var categoryFolder in categoryFolders.OrderBy(folder => folder.FullName, StringComparer.OrdinalIgnoreCase))
            categories.Add(await ReadCategoryAsync(categoryFolder));

        await client.DisconnectAsync(true);

        var (rules, issues) = DeriveRules(categories);
        var report = new Report(rootFolderName, sourceConfigPath, categories.ToArray(), rules, issues);
        Console.Out.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        Context.Shutdown();
    }

    private static async Task<SourceConfiguration> ReadSourceConfigurationAsync(string configuredPath)
    {
        var path = Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var config = document.RootElement;
        return new SourceConfiguration(
            config.GetProperty("imap.host").GetString()
                ?? throw new InvalidOperationException("imap.host is null."),
            config.GetProperty("imap.user").GetString()
                ?? throw new InvalidOperationException("imap.user is null."),
            config.GetProperty("imap.pwd").GetString()
                ?? throw new InvalidOperationException("imap.pwd is null."));
    }

    private static async Task<Category> ReadCategoryAsync(IMailFolder categoryFolder)
    {
        var children = await categoryFolder.GetSubfoldersAsync(false);
        var sampleFolder = children.FirstOrDefault(folder =>
            folder.Name.Equals("samples", StringComparison.OrdinalIgnoreCase));
        if (sampleFolder is null)
            return new Category(categoryFolder.FullName, $"{categoryFolder.FullName}.samples", 0, []);

        await sampleFolder.OpenAsync(FolderAccess.ReadOnly);
        var summaries = sampleFolder.Count == 0
            ? Array.Empty<IMessageSummary>()
            : (await sampleFolder.FetchAsync(0, -1, MessageSummaryItems.UniqueId)).ToArray();
        var samples = new List<Sample>();
        foreach (var summary in summaries)
        {
            var message = await sampleFolder.GetMessageAsync(summary.UniqueId);
            samples.Add(new Sample(
                summary.UniqueId.Id,
                message.Subject ?? "",
                message.From.ToString(),
                message.From.Mailboxes
                    .Select(mailbox => mailbox.Address)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(address => address, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                EmptyToNull(message.Headers[HeaderId.Sender]),
                EmptyToNull(message.ReplyTo.ToString()),
                EmptyToNull(message.Headers[HeaderId.ListId])));
        }
        await sampleFolder.CloseAsync(false);
        return new Category(categoryFolder.FullName, sampleFolder.FullName, samples.Count, samples.ToArray());
    }

    private static (Rule[] Rules, string[] Issues) DeriveRules(IEnumerable<Category> categories)
    {
        var issues = new List<string>();
        var candidates = new List<AddressCandidate>();
        foreach (var category in categories)
        {
            var addresses = category.Samples
                .SelectMany(sample => sample.FromAddresses)
                .Where(address => AddressDomain(address) is not null)
                .Select(address => address.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(address => address, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (category.SampleCount > 0 && addresses.Length == 0)
                issues.Add($"{category.SampleFolder} contains samples without a usable From mailbox address.");

            foreach (var address in addresses)
            {
                var domain = AddressDomain(address)!;
                var matchingSamples = category.Samples.Count(sample =>
                    sample.FromAddresses.Any(sampleAddress =>
                        string.Equals(sampleAddress, address, StringComparison.OrdinalIgnoreCase)));
                candidates.Add(new AddressCandidate(category.DestinationFolder, address, domain, matchingSamples));
            }
        }

        var ambiguousAddresses = candidates
            .GroupBy(candidate => candidate.Address, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(candidate => candidate.DestinationFolder)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var address in ambiguousAddresses.OrderBy(address => address, StringComparer.OrdinalIgnoreCase))
            issues.Add($"Sender address {address} occurs in multiple categories; no deterministic from rule can route it.");

        var promotedDomains = candidates
            .GroupBy(candidate => candidate.Domain, StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group.Select(candidate => candidate.Address).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 4 &&
                group.Select(candidate => candidate.DestinationFolder).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var group in candidates
                     .GroupBy(candidate => candidate.Domain, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Select(candidate => candidate.DestinationFolder)
                         .Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var destinations = group
                .Select(candidate => candidate.DestinationFolder)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(destination => destination, StringComparer.OrdinalIgnoreCase);
            issues.Add($"CONFLICTING DOMAIN RULE: Sender domain @{group.Key} occurs in multiple destination folders ({string.Join(", ", destinations)}); keeping only unambiguous explicit address rules.");
        }

        var addressRules = candidates
            .Where(candidate =>
                !ambiguousAddresses.Contains(candidate.Address) &&
                !promotedDomains.Contains(candidate.Domain))
            .Select(candidate => new Rule(
                candidate.DestinationFolder,
                "from",
                candidate.Address,
                candidate.SampleCount,
                $"MATCH {FormatDestination(candidate.DestinationFolder)} from {candidate.Address}"));
        var domainRules = candidates
            .Where(candidate => promotedDomains.Contains(candidate.Domain))
            .GroupBy(candidate => candidate.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(group => new Rule(
                group.First().DestinationFolder,
                "from",
                $"@{group.Key}",
                group.Sum(candidate => candidate.SampleCount),
                $"MATCH {FormatDestination(group.First().DestinationFolder)} from @{group.Key}"));
        var rules = addressRules
            .Concat(domainRules)
            .OrderBy(rule => rule.DestinationFolder, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return (rules, issues.ToArray());
    }

    private static string FormatDestination(string destinationFolder) =>
        destinationFolder.Any(char.IsWhiteSpace)
            ? JsonSerializer.Serialize(destinationFolder)
            : destinationFolder;

    private static string? AddressDomain(string address)
    {
        var separator = address.LastIndexOf('@');
        return separator >= 0 && separator < address.Length - 1
            ? address[(separator + 1)..].ToLowerInvariant()
            : null;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
