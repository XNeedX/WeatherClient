using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WeatherClient.Models;

namespace WeatherClient.Services
{
    public class WeatherService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey = "d2c77f56f411c56d25cd41be45ed4362";

        public async Task<WeatherResponse> GetWeatherAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return null;

            var url =
                $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric&lang=ru";

            try
            {
                var data = await _http.GetFromJsonAsync<WeatherResponse>(url);
                return data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ForecastResponse> GetForecastAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return null;

            // Обратите внимание на URL: /data/2.5/forecast
            var url =
                $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric&lang=ru";

            try
            {
                var data = await _http.GetFromJsonAsync<ForecastResponse>(url);
                return data;
            }
            catch
            {
                // В реальном проекте тут нужно логировать ошибку
                return null;
            }
        }
    }
}