using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using ScottPlot.WinUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using WeatherClient.Models;
using WeatherClient.ViewModels;

namespace WeatherClient.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private bool _firstActivationDone = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            DispatcherQueue? dq = this.DispatcherQueue;
            ViewModel = new MainViewModel(dq);

            if (this.Content is FrameworkElement root)
            {
                root.DataContext = ViewModel;
            }

            this.Activated += OnWindowActivated;
        }

        private async void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (_firstActivationDone) return;
            _firstActivationDone = true;
            this.Activated -= OnWindowActivated;

            try
            {
                await ViewModel.GetWeatherAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView.MenuItems.FirstOrDefault() is NavigationViewItem firstItem)
            {
                NavView.SelectedItem = firstItem;
                Navigate(firstItem.Tag.ToString());
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag.ToString();
                Navigate(tag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void Navigate(string navItemTag, NavigationTransitionInfo transitionInfo = null)
        {
            if (ContentFrame == null) return;

            transitionInfo ??= new EntranceNavigationTransitionInfo();

            DataTemplate template = navItemTag switch
            {
                "Current" => (DataTemplate)NavView.Resources["CurrentWeatherTemplate"],
                "Forecast" => (DataTemplate)NavView.Resources["ForecastTemplate"],
                "Charts" => (DataTemplate)NavView.Resources["ChartsTemplate"],
                "Favorites" => (DataTemplate)NavView.Resources["FavoritesTemplate"],
                _ => null
            };

            if (template == null) return;

            var contentPresenter = new ContentPresenter
            {
                ContentTemplate = template,
                Content = ViewModel
            };

            ContentFrame.Content = contentPresenter;

            // НАЗНАЧЕНИЕ ГРАФИКОВ
            if (navItemTag == "Current")
            {
                contentPresenter.Loaded += (s, e) =>
                {
                    // Находим HourlyPlot
                    var hourlyPlot = FindChild<WinUIPlot>(contentPresenter, "HourlyPlot");
                    if (hourlyPlot != null)
                    {
                        ViewModel.HourlyPlotControl = hourlyPlot;
                        // Отключаем масштабирование
                        hourlyPlot.UserInputProcessor.IsEnabled = false;
                        hourlyPlot.UserInputProcessor.IsEnabled = false;

                        // Обновляем график если есть данные
                        if (ViewModel.DailyForecasts.Any())
                        {
                            _ = ViewModel.GetWeatherAsync();
                        }
                    }
                };
            }
            else if (navItemTag == "Charts")
            {
                contentPresenter.Loaded += (s, e) =>
                {
                    var mainPlot = FindChild<WinUIPlot>(contentPresenter, "TemperatureChart");
                    if (mainPlot != null)
                    {
                        ViewModel.ChartControl = mainPlot;
                        if (ViewModel.DailyForecasts.Any())
                        {
                            _ = ViewModel.GetWeatherAsync();
                        }
                    }
                };
            }
        }

        // МОДИФИЦИРОВАННЫЙ метод поиска с указанием имени
        private static T FindChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);

                // Проверяем по имени если указано
                if (name != null && child is FrameworkElement fe && fe.Name == name)
                {
                    return child as T;
                }

                if (child is T typedChild && (name == null || (child as FrameworkElement)?.Name == name))
                    return typedChild;

                var found = FindChild<T>(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CityAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdateCitySuggestions(sender.Text);
            }
        }

        private void CityAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // Теперь ничего не делаем, так как выбор происходит через команду CitySelected
            sender.CloseFlyout();
        }
        private async void FavoriteButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true; // Блокируем всплытие события!

            if (sender is Button button && button.DataContext is CitySuggestionUiModel suggestion)
            {
                if (!ViewModel.FavoriteCities.Contains(suggestion.CityName))
                {
                    ViewModel.FavoriteCities.Add(suggestion.CityName);
                    suggestion.IsFavorite = true; // Обновляем модель напрямую

                    // Пересобираем список для упорядочивания
                    ViewModel.UpdateCitySuggestions(ViewModel.City);
                }
            }
        }
    }

    // Расширение для закрытия Flyout
    public static class FlyoutExtensions
    {
        public static void CloseFlyout(this AutoSuggestBox autoSuggestBox)
        {
            var flyout = autoSuggestBox.Parent as FlyoutPresenter;
            var button = flyout?.Parent as Button;
            button?.Flyout?.Hide();
        }
    }
}