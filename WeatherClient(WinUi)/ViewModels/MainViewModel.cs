using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherClient.Models;
using WeatherClient.Services;
using ScottPlot;

namespace WeatherClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WeatherService _weatherService = new();
        private readonly DispatcherQueue _dispatcher;
        private List<string> _allCities = new();

        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            City = "Москва";
            DailyForecasts = new ObservableCollection<DailyForecastUiModel>();
            CitySuggestions = new ObservableCollection<CitySuggestionUiModel>();
            SearchHistory = new ObservableCollection<string>();
            FavoriteCities = new ObservableCollection<string>();

            _ = LoadCityListAsync();
            UpdateFavoriteButtonState();
        }

        // ======== СВОЙСТВА ПОГОДЫ ========
        [ObservableProperty] private string city;
        [ObservableProperty] private string temperature;
        [ObservableProperty] private string description;
        [ObservableProperty] private string humidity;
        [ObservableProperty] private string pressure;
        [ObservableProperty] private string windSpeed;
        [ObservableProperty] private string sunrise;
        [ObservableProperty] private string sunset;
        [ObservableProperty] private string iconUrl;

        // ======== ДОПОЛНИТЕЛЬНЫЕ ПОКАЗАТЕЛИ ========
        [ObservableProperty] private string uvIndex;
        [ObservableProperty] private string visibility;
        [ObservableProperty] private string feelsLike;
        [ObservableProperty] private string currentDateTime;

        // ======== КОЛЛЕКЦИИ ========
        public ObservableCollection<DailyForecastUiModel> DailyForecasts { get; }
        [ObservableProperty] private ObservableCollection<CitySuggestionUiModel> citySuggestions;
        [ObservableProperty] private ObservableCollection<string> searchHistory;
        [ObservableProperty] public ObservableCollection<string> favoriteCities;

        // ======== КОНТРОЛЫ ========
        public ScottPlot.WinUI.WinUIPlot? ChartControl { get; set; }
        public ScottPlot.WinUI.WinUIPlot? HourlyPlotControl { get; set; }

        // ======== СОСТОЯНИЕ ========
        [ObservableProperty] private bool canAddToFavorites;
        [ObservableProperty] private string currentCityDisplayed = string.Empty;

        private async Task LoadCityListAsync()
        {
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "cities.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var cities = JsonSerializer.Deserialize<List<string>>(json);
                    if (cities != null)
                    {
                        _allCities = cities;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки городов: {ex.Message}");
            }

            _allCities = new List<string>
            {
                "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург",
                "Казань", "Нижний Новгород", "Челябинск", "Красноярск",
                "Самара", "Уфа", "Ростов-на-Дону", "Омск",
                "Краснодар", "Воронеж", "Пермь", "Волгоград",
                "Лондон", "Нью-Йорк", "Париж", "Токио", "Берлин"
            };
        }

        public void UpdateCitySuggestions(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _dispatcher.TryEnqueue(() => CitySuggestions.Clear());
                return;
            }

            var filtered = _allCities
                .Where(c => c.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                .Take(15)
                .Select(city => new CitySuggestionUiModel
                {
                    CityName = city,
                    IsFavorite = FavoriteCities.Contains(city)
                })
                .OrderByDescending(s => s.IsFavorite)
                .ThenBy(s => s.CityName)
                .ToList();

            _dispatcher.TryEnqueue(() =>
            {
                CitySuggestions.Clear();
                foreach (var suggestion in filtered) CitySuggestions.Add(suggestion);
            });
        }

        public void UpdateFavoriteButtonState()
        {
            CanAddToFavorites = !string.IsNullOrWhiteSpace(CurrentCityDisplayed) &&
                               !FavoriteCities.Contains(CurrentCityDisplayed);
        }

        [RelayCommand]
        public async Task GetWeatherAsync()
        {
            if (string.IsNullOrWhiteSpace(City)) return;

            if (!SearchHistory.Contains(City))
            {
                SearchHistory.Insert(0, City);
                if (SearchHistory.Count > 10) SearchHistory.RemoveAt(SearchHistory.Count - 1);
            }

            var currentWeatherTask = _weatherService.GetWeatherAsync(City);
            var forecastTask = _weatherService.GetForecastAsync(City);
            await Task.WhenAll(currentWeatherTask, forecastTask);

            var currentData = currentWeatherTask.Result;
            var forecastData = forecastTask.Result;

            _dispatcher.TryEnqueue(() =>
            {
                UpdateCurrentWeatherUi(currentData);
                UpdateForecastUi(forecastData);
            });
        }

        private void UpdateCurrentWeatherUi(WeatherResponse data)
        {
            if (data?.Weather?.Length > 0)
            {
                Temperature = $"{data.Main.Temp:F1}°C";
                Description = data.Weather[0].Description;
                Humidity = $"{data.Main.Humidity}%";
                Pressure = $"{data.Main.Pressure} гПа";
                WindSpeed = $"{data.Wind.Speed} м/с";

                // Обновление Feels Like из API
                FeelsLike = $"{data.Main.FeelsLike:F1}°C";

                // Обновление Visibility из API (метры → км)
                Visibility = $"{data.Visibility / 1000.0:F1} км";

                // Расчет UV Index (приближенный)
                UvIndex = CalculateUvIndex(data);

                var offset = TimeSpan.FromSeconds(data.Timezone);
                Sunrise = data.Sys.Sunrise.ToOffset(offset).ToString("HH:mm");
                Sunset = data.Sys.Sunset.ToOffset(offset).ToString("HH:mm");
                CurrentDateTime = DateTimeOffset.Now.ToOffset(offset).ToString("dddd, MMM dd | HH:mm", new CultureInfo("ru-RU"));

                IconUrl = $"https://openweathermap.org/img/wn/{data.Weather[0].Icon}@2x.png";

                CurrentCityDisplayed = City;
                UpdateFavoriteButtonState();
            }
        }

        private string CalculateUvIndex(WeatherResponse data)
        {
            if (data?.Weather?.Length == 0) return "N/A";

            var currentHour = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromSeconds(data.Timezone)).Hour;
            double baseUv = 0;

            if (currentHour >= 6 && currentHour <= 18)
            {
                var hourFromNoon = Math.Abs(currentHour - 12);
                baseUv = Math.Max(0, 8 - hourFromNoon * 1.3);
            }

            double cloudReduction = (data.Clouds.CloudCoverage / 25.0) * 1.0;
            double finalUv = Math.Max(0, baseUv - cloudReduction);
            finalUv = Math.Min(11, finalUv);

            return finalUv.ToString("F1");
        }

        private void UpdateForecastUi(ForecastResponse forecastData)
        {
            DailyForecasts.Clear();
            if (forecastData?.List == null) return;

            var groupedByDay = forecastData.List
                .GroupBy(x => x.DateTime.Date)
                .OrderBy(g => g.Key)
                .Take(7);

            foreach (var dayGroup in groupedByDay)
            {
                double minTemp = dayGroup.Min(x => x.Main.Temp);
                double maxTemp = dayGroup.Max(x => x.Main.Temp);
                var representativeItem = dayGroup.FirstOrDefault(x => x.DateTime.Hour >= 12) ?? dayGroup.First();

                DailyForecasts.Add(new DailyForecastUiModel
                {
                    DayName = dayGroup.Key.ToString("ddd", new CultureInfo("ru-RU")),
                    DateDisplay = dayGroup.Key.ToString("dd MMM", new CultureInfo("ru-RU")),
                    MinTemp = minTemp,
                    MaxTemp = maxTemp,
                    Description = representativeItem.Weather[0].Description,
                    IconUrl = $"https://openweathermap.org/img/wn/{representativeItem.Weather[0].Icon}@2x.png"
                });
            }

            UpdateCharts(forecastData);
        }

        private void UpdateCharts(ForecastResponse forecastData)
        {
            if (ChartControl?.Plot != null)
            {
                ChartControl.Plot.Clear();
                var items = forecastData.List.Take(24).ToList();

                if (items.Any())
                {
                    var dates = items.Select(i => i.DateTime.ToOADate()).ToArray();
                    var temps = items.Select(i => i.Main.Temp).ToArray();

                    var scatter = ChartControl.Plot.Add.Scatter(dates, temps);
                    scatter.LineWidth = 3;
                    scatter.Color = ScottPlot.Colors.Orange;
                    scatter.MarkerSize = 8;

                    ChartControl.Plot.Axes.DateTimeTicksBottom();
                    ChartControl.Plot.Title("Температура на 3 дня (шаг 3 часа)");
                    ChartControl.Plot.YLabel("Температура (°C)");
                    ChartControl.Refresh();
                }
            }

            if (HourlyPlotControl?.Plot != null)
            {
                HourlyPlotControl.Plot.Clear();
                var hourlyData = forecastData.List.Take(8).ToList();

                if (hourlyData.Any())
                {
                    var dates = hourlyData.Select(x => x.DateTime.ToOADate()).ToArray();
                    var temps = hourlyData.Select(x => x.Main.Temp).ToArray();

                    var scatter = HourlyPlotControl.Plot.Add.Scatter(dates, temps);
                    scatter.LineWidth = 2;
                    scatter.Color = ScottPlot.Colors.Blue;
                    scatter.MarkerSize = 6;

                    HourlyPlotControl.Plot.Axes.DateTimeTicksBottom();
                    HourlyPlotControl.Plot.Title("");
                    HourlyPlotControl.Plot.YLabel("°C");
                    HourlyPlotControl.Plot.HideGrid();
                    HourlyPlotControl.UserInputProcessor.IsEnabled = false;
                    HourlyPlotControl.Refresh();
                }
            }
        }

        [RelayCommand]
        public void AddToFavorites()
        {
            if (!string.IsNullOrWhiteSpace(CurrentCityDisplayed) &&
                !FavoriteCities.Contains(CurrentCityDisplayed))
            {
                FavoriteCities.Add(CurrentCityDisplayed);
                UpdateFavoriteButtonState();
            }
        }

        [RelayCommand]
        public void RemoveFromFavorites(string city)
        {
            if (FavoriteCities.Contains(city))
            {
                FavoriteCities.Remove(city);
                UpdateFavoriteButtonState();
            }
        }

        [RelayCommand]
        public async Task AddToFavoritesFromSuggestion(string? cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName) || FavoriteCities.Contains(cityName)) return;

            FavoriteCities.Add(cityName);
            UpdateFavoriteButtonState();

            await Task.Delay(50);
            UpdateCitySuggestions(City);
        }

        [RelayCommand]
        public void LoadCityFromFavorites(string city)
        {
            City = city;
            GetWeatherCommand.Execute(null);
        }

        [RelayCommand]
        public void LoadCityFromHistory(string city)
        {
            City = city;
            GetWeatherCommand.Execute(null);
        }

        [RelayCommand]
        public void CitySelected(string cityName)
        {
            if (!string.IsNullOrWhiteSpace(cityName))
            {
                City = cityName;
                GetWeatherCommand.Execute(null);
            }
        }
    }
}