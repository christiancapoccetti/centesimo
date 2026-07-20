using System.Globalization;

namespace Centesimo.App;

public sealed class CategoryIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var icon = value?.ToString();
        if (parameter?.ToString() == "Name")
            return icon switch
            {
                "cart" => "Carrello",
                "home" => "Casa",
                "car" => "Auto",
                "heart" => "Cuore",
                _ => "Altro"
            };

        return icon switch
        {
            "cart" => "▰",
            "home" => "⌂",
            "car" => "◆",
            "heart" => "♥",
            _ => "●"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}