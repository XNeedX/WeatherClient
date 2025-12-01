using CommunityToolkit.Mvvm.ComponentModel;

namespace WeatherClient.Models
{
    public partial class CitySuggestionUiModel : ObservableObject
    {
        public string CityName { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isFavorite;
    }
}