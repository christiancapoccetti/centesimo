namespace Centesimo.Application;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new("Error.None", "");
}
