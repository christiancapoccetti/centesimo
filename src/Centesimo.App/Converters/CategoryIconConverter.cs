using System.Globalization;

namespace Centesimo.App;

public sealed class CategoryIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() switch
        {
            "cart" => "Carrello",
            "home" => "Casa",
            "car" => "Auto",
            "heart" => "Cuore",
            "sport" => "Sport",
            "shopping-bag" => "Shopping",
            "food" => "Cibo",
            "gift" => "Regalo",
            "beach" => "Mare",
            "tech" => "Tecnologia",
            "school" => "Scuola",
            "family" => "Famiglia",
            "gym" => "Palestra",
            "gear" => "Ingranaggio",
            _ => "Altro"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CategoryIconSourceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ImageSource.FromFile($"category_{value?.ToString() switch
        {
            "cart" => "cart",
            "home" => "home",
            "car" => "car",
            "heart" => "heart",
            "sport" => "sport",
            "shopping-bag" => "shopping_bag",
            "food" => "food",
            "gift" => "gift",
            "beach" => "beach",
            "tech" => "tech",
            "school" => "school",
            "family" => "family",
            "gym" => "gym",
            "gear" => "gear",
            _ => "more"
        }}.svg");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
