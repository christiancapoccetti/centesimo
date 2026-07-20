using System.Globalization;
using Centesimo.Domain;

namespace Centesimo.Application;

public static class MoneyInputParser
{
    public static readonly Error InvalidMoney = new(
        "Money.InvalidInput",
        "Inserisci un importo valido con massimo due decimali.");

    public static Result<Money?> ParseOptional(string? value, CultureInfo culture)
    {
        if (value.IsEmpty())
            return Result<Money?>.Success(null);

        if (!decimal.TryParse(value, NumberStyles.Number, culture, out var amount) || amount < 0)
            return Result<Money?>.Failure(InvalidMoney);

        try
        {
            return Result<Money?>.Success(Money.FromDecimal(amount));
        }
        catch (ArgumentException)
        {
            return Result<Money?>.Failure(InvalidMoney);
        }
        catch (OverflowException)
        {
            return Result<Money?>.Failure(InvalidMoney);
        }
    }
}
