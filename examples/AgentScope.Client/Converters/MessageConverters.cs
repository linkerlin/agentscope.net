using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AgentScope.Client.Converters;

public class RoleToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role switch
            {
                "user" => new SolidColorBrush(Color.Parse("#E3F2FD")),
                "assistant" => new SolidColorBrush(Color.Parse("#F3E5F5")),
                "system" => new SolidColorBrush(Color.Parse("#FFF3E0")),
                "tool" => new SolidColorBrush(Color.Parse("#E8F5E9")),
                _ => new SolidColorBrush(Colors.White)
            };
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RoleToAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role switch
            {
                "user" => Avalonia.Layout.HorizontalAlignment.Right,
                "assistant" => Avalonia.Layout.HorizontalAlignment.Left,
                _ => Avalonia.Layout.HorizontalAlignment.Left
            };
        }
        return Avalonia.Layout.HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RoleToLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role switch
            {
                "user" => "你",
                "assistant" => "AI",
                "system" => "系统",
                "tool" => "工具",
                _ => role
            };
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isBusy && isBusy) return 0.5;
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
