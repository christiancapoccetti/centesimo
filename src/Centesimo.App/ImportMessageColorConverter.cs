using System.Globalization;

namespace Centesimo.App;

public sealed class ImportMessageColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#145C52");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
