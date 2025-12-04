using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace WeatherClient.Services
{
    public static class LocationService
    {
        private static readonly HttpClient _http = new();

        public static async Task<string> GetCityByLocationAsync()
        {
            // 1. координаты
            var locator = new Geolocator { DesiredAccuracyInMeters = 100 };
            var pos = await locator.GetGeopositionAsync();
            var lat = pos.Coordinate.Point.Position.Latitude;
            var lon = pos.Coordinate.Point.Position.Longitude;

            // 2. обратное гео OpenWeather
            var url = $"https://api.openweathermap.org/geo/1.0/reverse?" +
                      $"lat={lat}&lon={lon}&limit=1&appid={OpenWeatherApiKey}";

            var res = await _http.GetFromJsonAsync<GeoResponse[]>(url);
            return res?.Length > 0 ? res[0].LocalNames?.Ru ?? res[0].Name : null;
        }

        private const string OpenWeatherApiKey = "d2c77f56f411c56d25cd41be45ed4362"; // ваш ключ

        private class GeoResponse
        {
            public string Name { get; set; }
            public LocalNames LocalNames { get; set; }
        }
        private class LocalNames
        {
            public string Ru { get; set; }
        }
    }
}