using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AgentScope.Client.Converters;

/// <summary>
/// 根据 CurrentPageId 和 ConverterParameter 匹配返回高亮颜色。
/// 当 CurrentPageId 与 ConverterParameter 相等时返回紫色高亮，否则返回暗色。
/// </summary>
public class PageIdToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var currentId = value as string ?? string.Empty;
        var targetId = parameter as string ?? string.Empty;

        return string.Equals(currentId, targetId, StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(Color.Parse("#7C3AED"))
            : new SolidColorBrush(Color.Parse("#2D2D3F"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
