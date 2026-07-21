namespace Centesimo.Application;

public static class ZipEntryPathSafety
{
    public static bool IsSafe(string entryName) =>
        !Path.IsPathRooted(entryName) &&
        !entryName.Replace('\\', '/').Split('/').Any(part => part is ".." or "");
}
