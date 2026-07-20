using System.Globalization;

namespace Centesimo.App;

public sealed class CategoryColorNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() switch
        {
            "#176B5B" => "Verde",
            "#8B4A5D" => "Bordeaux",
            "#725C00" => "Ocra",
            "#4F5F7A" => "Blu ardesia",
            "#6A4F88" => "Viola",
            _ => "Colore categoria"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
