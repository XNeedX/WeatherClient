namespace WeatherClient.Models
{
    public class WeatherResponse
    {
        public WeatherDescription[] Weather { get; set; }
        public MainInfo Main { get; set; }
        public WindInfo Wind { get; set; }
        public SysInfo Sys { get; set; }
        public string Name { get; set; }
    }

    public class WeatherDescription
    {
        public string Main { get; set; }
        public string Description { get; set; }  
        public string Icon { get; set; }
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
        public long Sunrise { get; set; }
        public long Sunset { get; set; }
    }
}
