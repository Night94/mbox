using System.Reflection;
using Mbox;

var unitName = "sentence-poem";
var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var appUnitPath = Path.Combine(repoRoot, "units", "apps", unitName, $"{unitName}.app.md");
var configPath = Path.Combine(AppContext.BaseDirectory, "application.json");
var framework = Framework.Load(appUnitPath, configPath, Assembly.GetExecutingAssembly());
await framework.RunAsync();

static string FindRepoRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "units", "system", "kernel.v1.md")))
            return dir.FullName;
    throw new InvalidOperationException("Unable to find MBOX repository root (units/system/kernel.v1.md not found above " + start + ").");
}
