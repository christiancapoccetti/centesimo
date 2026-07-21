namespace Centesimo.Application;

public static class ZipEntryPathSafety
{
    public static bool IsSafe(string entryName)
    {
        var normalized = entryName.Replace('\\', '/').TrimEnd('/');
        return !Path.IsPathRooted(normalized) &&
            normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).All(part => part != "..");
    }

    public static bool HasExpectedModelLayout(IEnumerable<string> entryNames, string modelName) =>
        entryNames.Any(entry => entry.Replace('\\', '/') == $"{modelName}/am/final.mdl") &&
        entryNames.Any(entry => entry.Replace('\\', '/') == $"{modelName}/conf/model.conf");
}
