using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WeatherClient.ViewModels;
using System;
using System.Linq;
using ScottPlot.WinUI;

namespace WeatherClient.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private bool _firstActivationDone = false;

        public MainWindow()
        {
            this.InitializeComponent();

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

            // ����� Navigate("Current") ������ ������ � ��������� � NavView_Loaded

            try
            {
                // �������� ������ �������� �����, ��� ��� ��� �� ������� �� ���������
                await ViewModel.GetWeatherAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"������ �������� ��� ���������: {ex.Message}");
            }
        }

        // --- ����������� �����: ��������� ����� �������� NavView ---
        private void NavView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (NavView.MenuItems.FirstOrDefault() is NavigationViewItem firstItem)
            {
                NavView.SelectedItem = firstItem;

                // ����� �����������: ����������� ����� Navigate, ����� �������� NRE
                NavView.DispatcherQueue.TryEnqueue(() =>
                {
                    Navigate(firstItem.Tag.ToString());
                });
            }
        }
        // ------------------------------------------------------------

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
            if (ContentFrame == null)
            {
                System.Diagnostics.Debug.WriteLine("ОШИБКА: ContentFrame is null!");
                return;
            }

            DataTemplate template = navItemTag switch
            {
                "Current" => (DataTemplate)NavView.Resources["CurrentWeatherTemplate"],
                "Forecast" => (DataTemplate)NavView.Resources["ForecastTemplate"],
                "Charts" => (DataTemplate)NavView.Resources["ChartsTemplate"],
                _ => null
            };

            if (template == null) return;

            var contentPresenter = new ContentPresenter
            {
                ContentTemplate = template,
                Content = ViewModel
            };

            // Устанавливаем контент напрямую
            ContentFrame.Content = contentPresenter;

            // Для Charts ждём загрузки визуального дерева
            if (navItemTag == "Charts")
            {
                contentPresenter.Loaded += (s, e) =>
                {
                    var plotControl = FindChild<ScottPlot.WinUI.WinUIPlot>(contentPresenter);
                    if (plotControl != null)
                    {
                        ViewModel.ChartControl = plotControl;
                        if (ViewModel.DailyForecasts.Any())
                        {
                            _ = ViewModel.GetWeatherAsync();
                        }
                    }
                };
            }
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}