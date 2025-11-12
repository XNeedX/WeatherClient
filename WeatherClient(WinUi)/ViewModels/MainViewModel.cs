using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching; 
using System;
using System.Threading.Tasks;
using WeatherClient.Services;

namespace WeatherClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WeatherService _weatherService = new();

        private readonly DispatcherQueue _dispatcher;

        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher; 
            City = "Москва";
        }

        [ObservableProperty] private string city;
        [ObservableProperty] private string temperature;
        [ObservableProperty] private string description;
        [ObservableProperty] private string humidity;
        [ObservableProperty] private string pressure;
        [ObservableProperty] private string windSpeed;
        [ObservableProperty] private string sunrise;
        [ObservableProperty] private string sunset;
        [ObservableProperty] private string iconUrl;

        [RelayCommand]
        public async Task GetWeatherAsync()
        {
            var data = await _weatherService.GetWeatherAsync(City);
            if (data == null || data.Weather == null || data.Weather.Length == 0)
                return;

            void updateAction()
            {
                Temperature = $"{data.Main.Temp:F1}°C";
                Description = data.Weather[0].Description;
                Humidity = $"{data.Main.Humidity}%";
                Pressure = $"{data.Main.Pressure} гПа";
                WindSpeed = $"{data.Wind.Speed} м/с";
                Sunrise = DateTimeOffset.FromUnixTimeSeconds(data.Sys.Sunrise).ToLocalTime().ToString("HH:mm");
                Sunset = DateTimeOffset.FromUnixTimeSeconds(data.Sys.Sunset).ToLocalTime().ToString("HH:mm");
                IconUrl = $"https://openweathermap.org/img/wn/{data.Weather[0].Icon}@2x.png";
            }

            _dispatcher.TryEnqueue(updateAction);
        }
    }
}