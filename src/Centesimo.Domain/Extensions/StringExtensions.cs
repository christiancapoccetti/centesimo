namespace Centesimo.Domain;

public static class StringExtensions
{
    public static bool IsEmpty(this string? value) => string.IsNullOrWhiteSpace(value);

    public static bool HasValue(this string? value) => !value.IsEmpty();
}
