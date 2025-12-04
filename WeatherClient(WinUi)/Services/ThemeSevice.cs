using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WeatherClient.Models;
using Windows.UI; 
using Microsoft.UI.Xaml.Media; 

namespace WeatherClient.Services
{
    public static class ThemeService
    {
        private static Window _window;

        public static void Initialize(Window window)
        {
            _window = window;
        }

        public static void ApplyTheme(AppTheme theme)
        {
            if (_window?.Content is not FrameworkElement root) return;

            // ✅ Цвета как переменные
            var darkBackgroundColor = Color.FromArgb(255, 9, 49, 111);  // #09316f
            var darkCardColor = Color.FromArgb(255, 11, 55, 136);       // #0b3788

            // ✅ Меняем Application-wide ресурсы (ThemeResource ищет здесь)
            if (theme == AppTheme.Dark)
            {
                Application.Current.Resources["CardGradientBrush"] = new SolidColorBrush(darkCardColor);
            }
            else
            {
                Application.Current.Resources["CardGradientBrush"] = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = Color.FromArgb(255, 98, 176, 221), Offset = 0.0 },
                new GradientStop { Color = Color.FromArgb(255, 89, 166, 215), Offset = 0.5 },
                new GradientStop { Color = Color.FromArgb(255, 82, 157, 201), Offset = 1.0 }
            }
                };
            }

            // ✅ Обновляем фон Grid (как было)
            if (root is Grid grid)
            {
                grid.Background = theme == AppTheme.Dark
                    ? new SolidColorBrush(darkBackgroundColor)
                    : new LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0),
                        EndPoint = new Windows.Foundation.Point(0, 1),
                        GradientStops =
                        {
                    new GradientStop { Color = Color.FromArgb(255, 98, 176, 221), Offset = 0.0 },
                    new GradientStop { Color = Color.FromArgb(255, 74, 195, 235), Offset = 0.5 },
                    new GradientStop { Color = Color.FromArgb(255, 82, 157, 201), Offset = 1.0 }
                        }
                    };
            }

            // ✅ Принудительно обновляем UI (триггер для ThemeResource)
            root.RequestedTheme = root.RequestedTheme;
        }
    }
}