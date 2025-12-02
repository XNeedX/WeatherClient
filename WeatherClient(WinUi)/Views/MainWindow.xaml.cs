using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using ScottPlot.WinUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using WeatherClient.Models;
using WeatherClient.ViewModels;
using WinRT;

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

            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1400, Height = 900 });
                    if (appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                        presenter.IsMaximizable = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки размера: {ex.Message}");
            }

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
                Navigate("Current");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate("Current");
            WeatherButton.Background = Application.Current.Resources["SystemAccentColor"] as Microsoft.UI.Xaml.Media.Brush;
            SettingsButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate("Favorites");
            SettingsButton.Background = Application.Current.Resources["SystemAccentColor"] as Microsoft.UI.Xaml.Media.Brush;
            WeatherButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        private void Navigate(string navItemTag, NavigationTransitionInfo transitionInfo = null)
        {
            if (ContentFrame == null) return;

            transitionInfo ??= new EntranceNavigationTransitionInfo();

            var resources = (this.Content as FrameworkElement)?.Resources;
            if (resources == null) return;

            DataTemplate template = navItemTag switch
            {
                "Current" => resources["CurrentWeatherTemplate"] as DataTemplate,
                "Charts" => resources["ChartsTemplate"] as DataTemplate,
                "Favorites" => resources["FavoritesTemplate"] as DataTemplate,
                _ => null
            };

            if (template == null) return;

            var contentPresenter = new ContentPresenter
            {
                ContentTemplate = template,
                Content = ViewModel
            };

            ContentFrame.Content = contentPresenter;
        }

        private void HourlyPlot_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is WinUIPlot plot)
            {
                ViewModel.HourlyPlotControl = plot;
                plot.UserInputProcessor.IsEnabled = false;
                ViewModel.InitializeHourlyChart();
            }
        }

        private void TemperatureChart_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is WinUIPlot plot)
            {
                ViewModel.ChartControl = plot;
                ViewModel.InitializeMainChart();
            }
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
            if (args.ChosenSuggestion is CitySuggestionUiModel suggestion)
            {
                ViewModel.CitySelectedCommand.Execute(suggestion.CityName);
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                ViewModel.City = args.QueryText;
                ViewModel.GetWeatherCommand.Execute(null);
            }

            var flyoutPresenter = sender.Parent as FlyoutPresenter;
            var button = flyoutPresenter?.Parent as Button;
            button?.Flyout?.Hide();
        }
    }
}