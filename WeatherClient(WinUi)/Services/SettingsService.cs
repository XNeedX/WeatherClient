using WeatherClient.Models;
using Windows.Storage;

namespace WeatherClient.Services
{
    public static class SettingsService
    {
        private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

        public static TemperatureUnit TemperatureUnit
        {
            get => (TemperatureUnit)(LocalSettings.Values["TempUnit"] ?? (int)TemperatureUnit.Celsius);
            set => LocalSettings.Values["TempUnit"] = (int)value;
        }
        public static string DefaultCity
        {
            get => LocalSettings.Values["DefaultCity"] as string ?? "Москва";
            set => LocalSettings.Values["DefaultCity"] = value;
        }
        public static WindSpeedUnit WindSpeedUnit
        {
            get => (WindSpeedUnit)(LocalSettings.Values["WindUnit"] ?? (int)WindSpeedUnit.Ms);
            set => LocalSettings.Values["WindUnit"] = (int)value;
        }

        public static AppTheme AppTheme
        {
            get => (AppTheme)(LocalSettings.Values["AppTheme"] ?? (int)AppTheme.Light);
            set => LocalSettings.Values["AppTheme"] = (int)value;
        }


        // Хелперы конвертации
        public static double ConvertTemperature(double celsius, TemperatureUnit unit) => unit switch
        {
            TemperatureUnit.Celsius => celsius,
            TemperatureUnit.Fahrenheit => celsius * 9 / 5 + 32,
            TemperatureUnit.Kelvin => celsius + 273.15,
            _ => celsius
        };
    }
}