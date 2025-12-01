using Microsoft.UI.Xaml.Data;
using System;

namespace WeatherClient.Converters
{
    public class FavoriteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // E00A = залитая звезда, E006 = контур звезды
            return (bool)value ? "\uE00A" : "\uE006";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}