using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WeatherClient.Models;
using Windows.UI;

namespace WeatherClient.Services
{
    public static class ThemeService
    {
        private static Window _window;

        public static void Initialize(Window window) => _window = window;

        public static void ApplyTheme(AppTheme theme)
        {
            if (_window?.Content is not FrameworkElement root) return;

            // 1. Просто меняем тему корневого элемента – всё остальное
            //    (включая CardGradientBrush) пересчитается по ThemeDictionaries
            root.RequestedTheme = theme == AppTheme.Dark
                                  ? ElementTheme.Dark
                                  : ElementTheme.Light;

            // 2. Фон окна (Mica всё равно перекроет его, но на всякий случай)
            if (root is Grid grid)
            {
                grid.Background = theme == AppTheme.Dark
                    ? new SolidColorBrush(Color.FromArgb(255, 9, 49, 111))
                    : new SolidColorBrush(Color.FromArgb(255, 98, 176, 221));
            }
        }
    }
}