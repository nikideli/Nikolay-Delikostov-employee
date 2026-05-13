using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace EmployeePairFinder.Converters;

// Returns Visible when the bound string is non-empty, Collapsed otherwise.
// Used to show/hide the error and warning panels without a dedicated bool property.
[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
