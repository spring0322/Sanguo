using System;
using System.Globalization;
using System.Windows.Data;

namespace WorldOfTheThreeKingdomsEditor.Converters;

/// <summary>
/// 反转 bool 值的转换器
/// true → false, false → true
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }
}
