using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherClient.Models;
using WeatherClient.Services;

namespace WeatherClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WeatherService _weatherService = new();
        private readonly DispatcherQueue _dispatcher;
        private List<string> _allCities = new();
        private ForecastResponse _lastForecastData;
        private WeatherResponse _lastWeatherData;

        [ObservableProperty] private bool _isSettingsPanelOpen;
        [ObservableProperty] private TemperatureUnit _selectedTempUnit;
        [ObservableProperty] private WindSpeedUnit _selectedWindUnit;
        public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();

        partial void OnSelectedTempUnitChanged(TemperatureUnit value)
        {
            SettingsService.TemperatureUnit = value;
            ApplySettingsChanges();
        }

        partial void OnSelectedWindUnitChanged(WindSpeedUnit value)
        {
            SettingsService.WindSpeedUnit = value;
            ApplySettingsChanges();
        }

        private void ApplySettingsChanges()
        {
            if (_lastWeatherData == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                // Пересчитываем только значения, не метки
                var data = _lastWeatherData;
                var temp = SettingsService.ConvertTemperature(data.Main.Temp, _selectedTempUnit);
                Temperature = $"{temp:F1}°{GetTempSymbol()}";

                WindSpeed = FormatWindSpeed(data.Wind.Speed);
                FeelsLikeValue = $"{SettingsService.ConvertTemperature(data.Main.FeelsLike, _selectedTempUnit):F1}°{GetTempSymbol()}";

                UpdateChartsIfLoaded();
            });
        }

        // ======== КОНСТРУКТОР ========
        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _selectedTempUnit = SettingsService.TemperatureUnit;
            _selectedWindUnit = SettingsService.WindSpeedUnit;
            _selectedTheme = SettingsService.AppTheme;
            _isSettingsPanelOpen = false;
            City = SettingsService.DefaultCity;

            DailyForecasts = new ObservableCollection<DailyForecastUiModel>();
            CitySuggestions = new ObservableCollection<CitySuggestionUiModel>();
            SearchHistory = new ObservableCollection<string>();
            FavoriteCities = new ObservableCollection<string>();

            _ = LoadCityListAsync();
            UpdateFavoriteButtonState();
        }

        [RelayCommand]
        private void ToggleSettingsPanel()
        {
            IsSettingsPanelOpen = !IsSettingsPanelOpen;
        }

   

        // ======== СВОЙСТВА ПОГОДЫ ========
        [ObservableProperty] private string city;
        [ObservableProperty] private string temperature;
        [ObservableProperty] private string description;
        [ObservableProperty] private string humidityValue;
        [ObservableProperty] private string pressure;
        [ObservableProperty] private string windSpeed;
        [ObservableProperty] private string sunrise;
        [ObservableProperty] private string sunset;
        [ObservableProperty] private string iconUrl;
        [ObservableProperty] private string uvIndexValue;
        [ObservableProperty] private string visibilityValue;
        [ObservableProperty] private string feelsLikeValue;
        [ObservableProperty] private string currentDateTime;
        [ObservableProperty] private object currentWeatherAnimation;

        public ObservableCollection<DailyForecastUiModel> DailyForecasts { get; }
        [ObservableProperty] private ObservableCollection<CitySuggestionUiModel> citySuggestions;
        [ObservableProperty] private ObservableCollection<string> searchHistory;
        [ObservableProperty] public ObservableCollection<string> favoriteCities;
        [ObservableProperty] private AppTheme _selectedTheme;

        [ObservableProperty] private int genderIndex;
        [ObservableProperty] private int age = 25;
        [ObservableProperty] private int styleIndex;
        [ObservableProperty] private string styleAdviceText = "Нажми кнопку";


        public ScottPlot.WinUI.WinUIPlot? ChartControl { get; set; }
        public ScottPlot.WinUI.WinUIPlot? HourlyPlotControl { get; set; }

        public ForecastResponse? LastForecastData => _lastForecastData;

        [ObservableProperty] private bool canAddToFavorites;
        [ObservableProperty] private string currentCityDisplayed = string.Empty;

        // ======== ОСТАЛЬНОЙ КОД ========
        partial void OnSelectedThemeChanged(AppTheme value)
        {
            SettingsService.AppTheme = value;

            // ✅ КРИТИЧЕСКИ: применяем тему
            ThemeService.ApplyTheme(value);
        }
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
  "Москва",
  "Санкт-Петербург",
  "Новосибирск",
  "Екатеринбург",
  "Казань",
  "Нижний Новгород",
  "Челябинск",
  "Красноярск",
  "Самара",
  "Уфа",
  "Башкирия",
  "Ростов-на-Дону",
  "Омск",
  "Краснодар",
  "Воронеж",
  "Пермь",
  "Волгоград",
  "Саратов",
  "Тюмень",
  "Тольятти",
  "Ижевск",
  "Барнаул",
  "Ульяновск",
  "Иркутск",
  "Хабаровск",
  "Ярославль",
  "Владивосток",
  "Набережные Челны",
  "Оренбург",
  "Томск",
  "Кемерово",
  "Астрахань",
  "Пенза",
  "Липецк",
  "Тула",
  "Киров",
  "Чебоксары",
  "Калининград",
  "Брянск",
  "Курск",
  "Иваново",
  "Магнитогорск",
  "Тверь",
  "Ставрополь",
  "Сочи",
  "Белгород",
  "Нижний Тагил",
  "Архангельск",
  "Владимир",
  "Смоленск",
  "Калуга",
  "Вологда",
  "Саранск",
  "Чита",
  "Улан-Удэ",
  "Мурманск",
  "Благовещенск",
  "Владикавказ",
  "Йошкар-Ола",
  "Симферополь",
  "Севастополь",
  "Ялта",
  "Лондон",
  "Париж",
  "Нью-Йорк",
  "Токио",
  "Берлин",
  "Пекин",
  "Шанхай",
  "Гуанчжоу",
  "Шэньчжэнь",
  "Мехико",
  "Мумбаи",
  "Дели",
  "Сан-Паулу",
  "Каир",
  "Стамбул",
  "Торонто",
  "Мадрид",
  "Рим",
  "Сидней",
  "Мельбурн",
  "Сеул",
  "Бангкок",
  "Сингапур",
  "Дубай",
  "Амстердам",
  "Копенгаген",
  "Стокгольм",
  "Осло",
  "Гельсинки",
  "Женева",
  "Цюрих",
  "Вена",
  "Прага",
  "Варшава",
  "Будапешт",
  "Бухарест",
  "София",
  "Белград",
  "Загреб",
  "Афины",
  "Лиссабон",
  "Дублин",
  "Эдинбург",
  "Бирмингем",
  "Манчестер",
  "Вашингтон",
  "Чикаго",
  "Лос-Анджелес",
  "Сан-Франциско",
  "Бостон",
  "Филадельфия",
  "Майами",
  "Сиэтл",
  "Гонконг",
  "Макао",
  "Тайбэй",
  "Йоханнесбург",
  "Кейптаун",
  "Найроби",
  "Аддис-Абеба",
  "Алжир",
  "Тунис",
  "Касабланка",
  "Киншаса",
  "Анкара",
  "Иерусалим",
  "Тегеран",
  "Багдад",
  "Эр-Рияд",
  "Кувейт",
  "Бахрейн",
  "Хьюстон",
  "Даллас",
  "Филадельфия",
  "Феникс",
  "Сан-Антонио",
  "Сан-Диего",
  "Детройт",
  "Денвер",
  "Портленд",
  "Лас-Вегас",
  "Шарлотт",
  "Нэшвилл",
  "Оклахома-Сити",
  "Монреаль",
  "Ванкувер",
  "Оттава",
  "Калгари",
  "Эдмонтон",
  "Гвадалахара",
  "Монтеррей",
  "Пуэбла",
  "Рио-де-Жанейро",
  "Буэнос-Айрес",
  "Сантьяго",
  "Лима",
  "Богота",
  "Каракас",
  "Гваякиль",
  "Кито",
  "Монтевидео",
  "Асунсьон",
  "Ла-Пас",
  "Санта-Крус-де-ла-Сьерра",
  "Лагос",
  "Кано",
  "Абуджа",
  "Кампала",
  "Дар-эс-Салам",
  "Лусака",
  "Хараре",
  "Конакри",
  "Дакар",
  "Претория",
  "Дурбан",
  "Порт-Элизабет",
  "Брисбен",
  "Перт",
  "Аделаида",
  "Канберра",
  "Голд-Кост",
  "Окленд",
  "Веллингтон",
  "Брюссель",
  "Люксембург",
  "Монако",
  "Мюнхен",
  "Гамбург",
  "Франкфурт",
  "Кельн",
  "Дюссельдорф",
  "Штутгарт",
  "Бремен",
  "Лейпциг",
  "Дрезден",
  "Нюрнберг",
  "Роттердам",
  "Гаага",
  "Утрехт",
  "Эйндховен",
  "Гент",
  "Антверпен",
  "Брюгге",
  "Страсбург",
  "Лион",
  "Марсель",
  "Ницца",
  "Бордо",
  "Тулуза",
  "Лилль",
  "Нант",
  "Милан",
  "Турин",
  "Генуя",
  "Венеция",
  "Флоренция",
  "Болонья",
  "Неаполь",
  "Палермо",
  "Барселона",
  "Валенсия",
  "Севилья",
  "Сарагоса",
  "Малага",
  "Порту",
  "Корк",
  "Рейкьявик",
  "Таллин",
  "Рига",
  "Вильнюс",
  "Краков",
  "Вроцлав",
  "Познань",
  "Гданьск",
  "Лодзь",
  "Дебрецен",
  "Сегед",
  "Брно",
  "Братислава",
  "Клуж-Напока",
  "Тимишоара",
  "Ясси",
  "Пловдив",
  "Варна",
  "Нови-Сад",
  "Сплит",
  "Сараево",
  "Скопье",
  "Тирана",
  "Кишинев",
  "Харбин",
  "Чанчунь",
  "Шэньян",
  "Далянь",
  "Тяньцзинь",
  "Циндао",
  "Нанкин",
  "Ханчжоу",
  "Сучжоу",
  "Вэньчжоу",
  "Нинбо",
  "Фучжоу",
  "Сямэнь",
  "Чэнду",
  "Чунцинь",
  "Ухань",
  "Чанша",
  "Чжэнчжоу",
  "Сиань",
  "Киото",
  "Осака",
  "Йокогама",
  "Нагоя","Саппоро","Фукуока","Кобе","Пусан","Инчхон","Дэгу","Датель","Ульсан","Ханой","Хошимин","Дананг","Хайфон","Чиангмай","Пхукет","Джакарта","Сурабая","Бандунг","Манила","Себу","Куала-Лумпур","Джорджтаун","Янгон","Мандалай","Дакка","Читтагонг","Коломбо","Катманду","Покхара","Тхимпху","Мале","Бангалор","Ченнай","Хайдарабад","Пуна","Ахмедабад","Калькутта","Джайпур","Лакхнау","Варанаси","Агра","Кочин","Тривандрум","Исламабад","Карачи","Лахор","Фейсалабад","Ташкент","Самарканд","Бухара","Алма-Ата","Астана","Шымкент","Караганда","Бишкек","Ош","Душанбе","Ашхабад","Измир","Бурса","Анталья","Исфахан","Мешхед","Шираз","Басра","Эрбиль","Джидда","Мекка","Медина","Эд-Даммам","Доха","Абу-Даби","Шарджа","Амман","Дамаск","Алеппо","Хомс","Бейрут","Тель-Авив","Хайфа","Александрия","Гиза","Луксор","Сфакс","Оран","Константина","Рабат","Марракеш","Фес","Танжер","Новополоцк","Полоцк","Маусинрам"
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

            _lastWeatherData = currentData;
            _lastForecastData = forecastData;

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
                var temp = SettingsService.ConvertTemperature(data.Main.Temp, _selectedTempUnit);
                Temperature = $"{temp:F1}°{GetTempSymbol()}";

                Description = data.Weather[0].Description;
                humidityValue = $"{data.Main.Humidity}%";
                Pressure = $"{data.Main.Pressure} гПа";

                WindSpeed = FormatWindSpeed(data.Wind.Speed);
                FeelsLikeValue = $"{SettingsService.ConvertTemperature(data.Main.FeelsLike, _selectedTempUnit):F1}°{GetTempSymbol()}";
                VisibilityValue = $"{data.Visibility / 1000.0:F1} км";

                UvIndexValue = CalculateUvIndex(data);

                var offset = TimeSpan.FromSeconds(data.Timezone);
                Sunrise = data.Sys.Sunrise.ToOffset(offset).ToString("HH:mm");
                Sunset = data.Sys.Sunset.ToOffset(offset).ToString("HH:mm");
                CurrentDateTime = DateTimeOffset.Now.ToOffset(offset).ToString("dddd, MMM dd | HH:mm", new CultureInfo("ru-RU"));

                IconUrl = $"https://openweathermap.org/img/wn/{data.Weather[0].Icon}@2x.png";

                CurrentCityDisplayed = City;
                UpdateFavoriteButtonState();

                _ = LoadAnimationSafely(data.Weather[0].Main, data.Weather[0].Description ?? "");
            }
        }

        private string GetTempSymbol() => _selectedTempUnit switch
        {
            TemperatureUnit.Celsius => "C",
            TemperatureUnit.Fahrenheit => "F",
            TemperatureUnit.Kelvin => "K",
            _ => "C"
        };

        private string FormatWindSpeed(double ms) => _selectedWindUnit switch
        {
            WindSpeedUnit.Ms => $"{ms:F1} м/с",
            WindSpeedUnit.Kmh => $"{ms * 3.6:F1} км/ч",
            WindSpeedUnit.Knots => $"{ms * 1.94384:F1} узл",
            _ => $"{ms:F1} м/с"
        };

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

        private async Task LoadAnimationSafely(string weatherMain, string description)
        {
            try
            {
                await Task.Delay(200);
                _dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        var animation = GetAnimationByWeather(weatherMain, description);
                        CurrentWeatherAnimation = animation;
                    }
                    catch
                    {
                        CurrentWeatherAnimation = null;
                    }
                });
            }
            catch
            {
                _dispatcher.TryEnqueue(() => CurrentWeatherAnimation = null);
            }
        }

        private object GetAnimationByWeather(string weatherMain, string description)
        {
            try
            {
                var main = weatherMain?.ToLower() ?? "";
                var desc = description?.ToLower() ?? "";

                var key = main switch
                {
                    "clear" => "SunnyAnimation",
                    "clouds" when desc.Contains("few") ||
                                 desc.Contains("scattered") ||
                                 desc.Contains("partly") ||
                                 desc.Contains("облачно с прояснениями") ||
                                 desc.Contains("переменная облачность") ||
                                 desc.Contains("небольшая облачность") =>
                                 "PartlyCloudyAnimation",
                    "clouds" => "CloudyAnimation",
                    "rain" or "drizzle" => "RainyAnimation",
                    "snow" => "SnowyAnimation",
                    "thunderstorm" => "ThunderAnimation",
                    _ => "SunnyAnimation"
                };

                if (Microsoft.UI.Xaml.Application.Current.Resources.ContainsKey(key))
                {
                    return Microsoft.UI.Xaml.Application.Current.Resources[key];
                }

                System.Diagnostics.Debug.WriteLine($"⚠️ Ресурс {key} не найден в Application.Resources");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
                return null;
            }
        }

        private void UpdateForecastUi(ForecastResponse forecastData)
        {
            DailyForecasts.Clear();
            if (forecastData?.List == null) return;

            _lastForecastData = forecastData;

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

            var hourly = forecastData.List.Take(12).Select(f => new HourlyItem
            {
                Time = f.DateTime.ToString("HH:mm"),
                Temp = $"{f.Main.Temp:F0}°",
                IconUrl = $"https://openweathermap.org/img/wn/{f.Weather[0].Icon}.png"
            });
            foreach (var h in hourly) HourlyForecast.Add(h);

            UpdateChartsIfLoaded();
        }

        public void InitializeHourlyChart()
        {
            if (HourlyPlotControl?.Plot == null) return;

            HourlyPlotControl.Plot.Axes.Color(ScottPlot.Colors.Gray);
            HourlyPlotControl.Plot.Grid.MajorLineColor = ScottPlot.Colors.Gray.WithOpacity(0.1);

            HourlyPlotControl.Plot.Title("");
            HourlyPlotControl.Plot.YLabel("°C");
            HourlyPlotControl.Plot.XLabel("");
            HourlyPlotControl.Plot.HideGrid();

            if (_lastForecastData != null)
            {
                UpdateHourlyChart(_lastForecastData);
            }

            HourlyPlotControl.Refresh();
        }

        public void InitializeMainChart()
        {
            if (ChartControl?.Plot == null) return;

            ChartControl.Plot.Axes.Color(ScottPlot.Colors.Gray);
            ChartControl.Plot.Grid.MajorLineColor = ScottPlot.Colors.Gray.WithOpacity(0.1);

            ChartControl.Plot.Title("Температура на 3 дня (шаг 3 часа)");
            ChartControl.Plot.YLabel("Температура (°C)");
            ChartControl.Plot.XLabel("");

            if (_lastForecastData != null)
            {
                UpdateMainChart(_lastForecastData);
            }

            ChartControl.Refresh();
        }

        private void UpdateChartsIfLoaded()
        {
            if (HourlyPlotControl?.Plot != null && _lastForecastData != null)
            {
                UpdateHourlyChart(_lastForecastData);
            }

            if (ChartControl?.Plot != null && _lastForecastData != null)
            {
                UpdateMainChart(_lastForecastData);
            }
        }

        private void UpdateHourlyChart(ForecastResponse forecastData)
        {
            if (HourlyPlotControl?.Plot == null) return;

            HourlyPlotControl.Plot.Clear();
            var hourlyData = forecastData.List.Take(8).ToList();

            if (hourlyData.Any())
            {
                var dates = hourlyData.Select(x => x.DateTime.ToOADate()).ToArray();
                var temps = hourlyData.Select(x => x.Main.Temp).ToArray();

                var scatter = HourlyPlotControl.Plot.Add.Scatter(dates, temps);
                scatter.LineWidth = 3;
                scatter.Color = ScottPlot.Colors.Blue;
                scatter.MarkerSize = 8;
                scatter.MarkerShape = ScottPlot.MarkerShape.OpenCircle;
                scatter.MarkerColor = ScottPlot.Colors.Blue;

                HourlyPlotControl.Plot.Axes.DateTimeTicksBottom();
                HourlyPlotControl.Plot.YLabel("°C");
                HourlyPlotControl.UserInputProcessor.IsEnabled = false;

                HourlyPlotControl.Refresh();
            }
        }

        private void UpdateMainChart(ForecastResponse forecastData)
        {
            if (ChartControl?.Plot == null) return;

            ChartControl.Plot.Clear();
            var items = forecastData.List.Take(24).ToList();

            if (items.Any())
            {
                var dates = items.Select(i => i.DateTime.ToOADate()).ToArray();
                var temps = items.Select(i => i.Main.Temp).ToArray();

                var scatter = ChartControl.Plot.Add.Scatter(dates, temps);
                scatter.LineWidth = 4;
                scatter.Color = ScottPlot.Colors.Red;
                scatter.MarkerSize = 10;
                scatter.MarkerShape = ScottPlot.MarkerShape.OpenCircle;
                scatter.MarkerColor = ScottPlot.Colors.Red;

                ChartControl.Plot.Axes.DateTimeTicksBottom();

                ChartControl.Refresh();
            }
        }

        // ======== КОМАНДЫ ========
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

        [RelayCommand]
        private async Task GetStyleAdvice()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(City) || _lastWeatherData == null)
                {
                    StyleAdviceText = "Сначала загрузите погоду 🙂";
                    return;
                }

                var todayItem = _lastForecastData?.List?
                                .OrderBy(f => f.DateTime)
                                .FirstOrDefault();

                var todayPop = todayItem?.PrecipitationProbability ?? 0;

                StyleAdviceText = "Думаю...";

                var req = new StyleAdviceRequest
                {
                    City = City,
                    Temp = SettingsService.ConvertTemperature(_lastWeatherData.Main.Temp, SelectedTempUnit),
                    WindSpeed = _lastWeatherData.Wind.Speed,
                    Description = _lastWeatherData.Weather[0].Description,
                    Gender = GenderIndex == 0 ? "мужчина" : "женщина",
                    Age = Age,
                    Pop = todayPop,
                    Style = StyleIndex switch
                    {
                        0 => "casual",
                        1 => "business",
                        2 => "sport",
                        _ => "grunge"
                    }
                };

                StyleAdviceText = await OpenRouterService.AskWeatherStyleAsync(req);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                StyleAdviceText = "Лимит исчерпан, попробуй позже.";
            }
            catch (Exception ex)
            {
                // дружное сообщение пользователю
                StyleAdviceText = $"Ошибка: {ex.Message}";
            }
        }
    }
}