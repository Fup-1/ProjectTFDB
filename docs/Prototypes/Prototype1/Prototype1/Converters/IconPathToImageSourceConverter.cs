using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ProjectTFDB.Converters;

public sealed class IconPathToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s)) return null;

            Uri uri;
            if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(s, UriKind.Absolute);
            }
            else
            {
                if (!File.Exists(s)) return null;
                uri = new Uri(s, UriKind.Absolute);
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = uri;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

