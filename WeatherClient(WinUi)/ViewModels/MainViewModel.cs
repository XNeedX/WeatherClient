using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using ScottPlot; // Добавляем ScottPlot
using System;
using System.Collections.Generic; // Нужно для List
using System.Collections.ObjectModel; // Нужно для ObservableCollection
using System.Globalization;
using System.Linq; // Нужно для группировки (GroupBy)
using System.Threading.Tasks;
using WeatherClient.Models;
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
            // Инициализируем коллекции
            DailyForecasts = new ObservableCollection<DailyForecastUiModel>();
        }

        // --- Свойства текущей погоды (ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ) ---
        [ObservableProperty] private string city;
        [ObservableProperty] private string temperature;
        [ObservableProperty] private string description;
        [ObservableProperty] private string humidity;
        [ObservableProperty] private string pressure;
        [ObservableProperty] private string windSpeed;
        [ObservableProperty] private string sunrise;
        [ObservableProperty] private string sunset;
        [ObservableProperty] private string iconUrl;

        // --- НОВЫЕ СВОЙСТВА ---

        // Коллекция для списка прогнозов по дням (для UI)
        public ObservableCollection<DailyForecastUiModel> DailyForecasts { get; }

        // Свойство для хранения графика (ссылка на контрол во View)
        // Мы будем передавать его из View в ViewModel
        public ScottPlot.WinUI.WinUIPlot ChartControl { get; set; }

        [RelayCommand]
        public async Task GetWeatherAsync()
        {
            // 1. Запрашиваем текущую погоду
            var currentWeatherTask = _weatherService.GetWeatherAsync(City);
            // 2. Запрашиваем прогноз
            var forecastTask = _weatherService.GetForecastAsync(City);

            // Ждем выполнения обеих задач параллельно
            await Task.WhenAll(currentWeatherTask, forecastTask);

            var currentData = currentWeatherTask.Result;
            var forecastData = forecastTask.Result;

            _dispatcher.TryEnqueue(() =>
            {
                // Обновляем текущую погоду (как и раньше, с учетом фикса времени)
                UpdateCurrentWeatherUi(currentData);

                // Обновляем прогноз и графики
                UpdateForecastUi(forecastData);
            });
        }

        private void UpdateCurrentWeatherUi(WeatherResponse data)
        {
            if (data == null || data.Weather == null || data.Weather.Length == 0) return;

            Temperature = $"{data.Main.Temp:F1}°C";
            Description = data.Weather[0].Description;
            Humidity = $"{data.Main.Humidity}%";
            Pressure = $"{data.Main.Pressure} гПа";
            WindSpeed = $"{data.Wind.Speed} м/с";

            var offset = TimeSpan.FromSeconds(data.Timezone);
            Sunrise = data.Sys.Sunrise.ToOffset(offset).ToString("HH:mm");
            Sunset = data.Sys.Sunset.ToOffset(offset).ToString("HH:mm");
            IconUrl = $"https://openweathermap.org/img/wn/{data.Weather[0].Icon}@2x.png";
        }

        private void UpdateForecastUi(ForecastResponse forecastData)
        {
            DailyForecasts.Clear();
            if (forecastData?.List == null) return;

            // --- ЛОГИКА ГРУППИРОВКИ ПРОГНОЗА ---

            // Группируем 3-часовые записи по дате (день месяца)
            var groupedByDay = forecastData.List
                .GroupBy(x => x.DateTime.Date)
                .OrderBy(g => g.Key)
                .Take(5); // Берем следующие 5 дней

            foreach (var dayGroup in groupedByDay)
            {
                // Вычисляем мин и макс температуру за этот день
                double minTemp = dayGroup.Min(x => x.Main.Temp);
                double maxTemp = dayGroup.Max(x => x.Main.Temp);

                // Пытаемся найти иконку и описание для середины дня (например, около 12:00-15:00),
                // чтобы они были репрезентативными. Если нет, берем первую.
                var representativeItem = dayGroup.FirstOrDefault(x => x.DateTime.Hour >= 12) ?? dayGroup.First();

                DailyForecasts.Add(new DailyForecastUiModel
                {
                    // CultureInfo ("ru-RU") нужна чтобы названия дней были на русском
                    DayName = dayGroup.Key.ToString("ddd", new CultureInfo("ru-RU")),
                    DateDisplay = dayGroup.Key.ToString("dd MMM", new CultureInfo("ru-RU")),
                    MinTemp = minTemp,
                    MaxTemp = maxTemp,
                    Description = representativeItem.Weather[0].Description,
                    IconUrl = $"https://openweathermap.org/img/wn/{representativeItem.Weather[0].Icon}@2x.png"
                });
            }

            // --- ЛОГИКА ОБНОВЛЕНИЯ ГРАФИКА ---
            if (ChartControl != null)
            {
                ChartControl.Plot.Clear();

                // Подготавливаем данные для графика: все 3-часовые точки
                List<DateTime> dates = new();
                List<double> temps = new();

                // Берем первые ~24 точки (3 дня * 8 точек), чтобы не перегружать график
                foreach (var item in forecastData.List.Take(24))
                {
                    dates.Add(item.DateTime);
                    temps.Add(item.Main.Temp);
                }

                // Добавляем график рассеяния (Scatter Plot) - точки, соединенные линией
                var scatter = ChartControl.Plot.Add.Scatter(dates.Select(d => d.ToOADate()).ToArray(), temps.ToArray());
                scatter.LineWidth = 3;
                scatter.Color = Colors.Orange; // Используем цвет из ScottPlot.Colors
                scatter.MarkerSize = 10;

                // Настраиваем оси
                ChartControl.Plot.Axes.DateTimeTicksBottom(); // Ось X - это даты/время
                ChartControl.Plot.Title("Температура на 3 дня (шаг 3 часа)");
                ChartControl.Plot.YLabel("Температура (°C)");

                // Обновляем отображение графика
                ChartControl.Refresh();
            }
        }
    }
}