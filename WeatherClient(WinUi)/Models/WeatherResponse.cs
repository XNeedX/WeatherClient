using System;
using System.Text.Json.Serialization;
using WeatherClient.Converters;

namespace WeatherClient.Models
{
    public class WeatherResponse
    {
        public WeatherDescription[] Weather { get; set; } = new WeatherDescription[0];
        public MainInfo Main { get; set; } = new MainInfo();
        public WindInfo Wind { get; set; } = new WindInfo();
        public SysInfo Sys { get; set; } = new SysInfo();
        public HourlyForecast forecast { get; set; } = new HourlyForecast();
        public long Timezone { get; set; }
        public string Name { get; set; } = new("");
    }

    public class WeatherDescription
    {
        public string Main { get; set; } = new("");
        public string Description { get; set; } = new("");
        public string Icon { get; set; } = new("");
    }

    public class MainInfo
    {
        public double Temp { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
    }

    public class WindInfo
    {
        public double Speed { get; set; }
    }

    public class SysInfo
    {
        // Раскомментировали атрибут и сменили тип на DateTimeOffset
        [JsonConverter(typeof(UnixEpochConverter))]
        public DateTimeOffset Sunrise { get; set; }

        // Раскомментировали атрибут и сменили тип на DateTimeOffset
        [JsonConverter(typeof(UnixEpochConverter))]
        public DateTimeOffset Sunset { get; set; }
    }

    public class HourlyForecast : MainInfo
    {
        public DateTimeOffset Time { get; set; }
    }
}