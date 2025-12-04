using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
// Мы можем переиспользовать часть старых моделей
// убедитесь, что namespace совпадает
namespace WeatherClient.Models
{
    // Главный объект ответа API прогноза
    public class ForecastResponse
    {
        [JsonPropertyName("list")]
        public List<ForecastItem> List { get; set; }

        [JsonPropertyName("city")]
        public CityInfo City { get; set; }
    }

    public class CityInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("timezone")]
        public long Timezone { get; set; }
    }

    // Элемент прогноза (один 3-часовой интервал)
    public class ForecastItem
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; } // Unix timestamp

        [JsonPropertyName("main")]
        public MainInfo Main { get; set; } // Переиспользуем из WeatherResponse.cs

        [JsonPropertyName("weather")]
        public WeatherDescription[] Weather { get; set; } // Переиспользуем

        [JsonPropertyName("wind")]
        public WindInfo Wind { get; set; } // Переиспользуем

        [JsonPropertyName("dt_txt")]
        public string DtTxt { get; set; } // Текстовое время "2023-10-27 12:00:00"

        [JsonPropertyName("pop")]
        public double PrecipitationProbability { get; set; }

        // Вспомогательное свойство для удобной работы с датой
        public DateTime DateTime => DateTime.Parse(DtTxt);
    }

    // --- Модель для отображения в UI (сводка за день) ---
    // Эту модель мы будем создавать сами во ViewModel
    public class DailyForecastUiModel
    {
        public string DayName { get; set; } // "Пн", "Вт" и т.д.
        public string DateDisplay { get; set; } // "27 окт"
        public double MaxTemp { get; set; }
        public double MinTemp { get; set; }
        public string IconUrl { get; set; }
        public string Description { get; set; }
        // Форматированные строки для UI
        public double MoonPhase { get; set; }
        public string MaxTempStr => $"{MaxTemp:F0}°";
        public string MinTempStr => $"{MinTemp:F0}°";
    }
}