using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using ScottPlot.WinUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using WeatherClient.Models;
using WeatherClient.Services;
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

            DispatcherQueue dq = this.DispatcherQueue;
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
                if (string.IsNullOrWhiteSpace(SettingsService.DefaultCity) ||
    SettingsService.DefaultCity == "Москва")
                {
                    try
                    {
                        var city = await LocationService.GetCityByLocationAsync();
                        if (!string.IsNullOrWhiteSpace(city))
                        {
                            ViewModel.City = city;
                            SettingsService.DefaultCity = city;   
                            await ViewModel.GetWeatherAsync();    
                        }
                    }
                    catch { }
                }
                Navigate("Current");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void AiButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSettingsPanelOpen = false;
            AiPanel.Visibility = Visibility.Visible;

            // контролы уже привязаны – просто синхронизируем начальные значения
            ViewModel.GenderIndex = SettingsService.GenderIndex;
            ViewModel.Age = SettingsService.Age;
            ViewModel.StyleIndex = SettingsService.StyleIndex;
        }

        private void CloseAiPanel_Click(object sender, RoutedEventArgs e)
            => AiPanel.Visibility = Visibility.Collapsed;

        // клик вне окна – тоже закрываем
        private void AiPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource == AiPanel)   // только фон
                AiPanel.Visibility = Visibility.Collapsed;
        }
        // подавляем всплытие при клике внутри окна
        private void AiBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
            => e.Handled = true;

        private void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Сначала закрываем панель настроек
            ViewModel.IsSettingsPanelOpen = false;

            // Затем переходим на главный экран
            Navigate("Current");
        }

        // ✅ ЕДИНСТВЕННЫЙ ОБРАБОТЧИК НАСТРОЕК БЕЗ ДУБЛИКАТОВ
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Настройки открыты");
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

        private void HourlyScroll_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            int delta = e.GetCurrentPoint(sv).Properties.MouseWheelDelta;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - delta);
            e.Handled = true;   // подавляем системную прокрутку
        }

        private void DailyScroll_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            int delta = e.GetCurrentPoint(sv).Properties.MouseWheelDelta;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - delta);
            e.Handled = true;
        }

        private void ChartsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSettingsPanelOpen = false;
            Navigate("Charts");          // тот же метод, что и для "Current"/"Favorites"
        }

        private void PressureChart_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScottPlot.WinUI.WinUIPlot plt || ViewModel?.LastForecastData == null) return;

            var data = ViewModel.LastForecastData;
            var xs = data.List.Select(f => f.DateTime.ToOADate()).ToArray();
            var press = data.List.Select(f => f.Main.Pressure * 0.75006).ToArray(); // мм рт. ст.

            plt.Plot.Clear();
            plt.Plot.Add.Scatter(xs, press, color: ScottPlot.Colors.Green);
            plt.Plot.YLabel("мм рт. ст.");
            plt.Plot.Axes.DateTimeTicksBottom();
            plt.Plot.Grid.IsVisible = true;
            plt.Refresh();
        }

        // Вероятность осадков
        private void PopChart_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScottPlot.WinUI.WinUIPlot plt || ViewModel?.LastForecastData == null) return;

            var data = ViewModel.LastForecastData;
            var xs = data.List.Select(f => f.DateTime.ToOADate()).ToArray();
            var pops = data.List.Select(f => f.PrecipitationProbability * 100).ToArray(); // 0-100 %

            plt.Plot.Clear();
            var bars = plt.Plot.Add.Bars(xs, pops);
            bars.Color = ScottPlot.Colors.DodgerBlue.WithAlpha(180);
            plt.Plot.Add.HorizontalLine(50, color: ScottPlot.Colors.Gray,
                               pattern: ScottPlot.LinePattern.Dashed);
            plt.Plot.YLabel("%");
            plt.Plot.Axes.DateTimeTicksBottom();
            plt.Plot.Grid.IsVisible = true;
            plt.Refresh();
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