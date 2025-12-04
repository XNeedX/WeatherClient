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
        public long Timezone { get; set; }
        public string Name { get; set; } = "";

        [JsonPropertyName("moon_ico")]   // для OneCall /daily/0/moon_ico
        public string MoonIconUrl { get; set; }

        public CloudsInfo Clouds { get; set; } = new CloudsInfo();

        public int Visibility { get; set; }
    }

    public class WeatherDescription
    {
        public string Main { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class MainInfo
    {
        public double Temp { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }

        // ДОБАВИЛИ: температура по ощущениям
        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }
    }

    public class WindInfo
    {
        public double Speed { get; set; }
    }

    public class SysInfo
    {
        [JsonConverter(typeof(UnixEpochConverter))]
        public DateTimeOffset Sunrise { get; set; }

        [JsonConverter(typeof(UnixEpochConverter))]
        public DateTimeOffset Sunset { get; set; }
    }

    public class HourlyForecast : MainInfo
    {
        public DateTimeOffset Time { get; set; }
    }

    public class CloudsInfo
    {
        [JsonPropertyName("all")]
        public int CloudCoverage { get; set; }
    }

    public class HourlyItem
    {
        public string Time { get; set; }
        public string Temp { get; set; }
        public string IconUrl { get; set; }
    }
}