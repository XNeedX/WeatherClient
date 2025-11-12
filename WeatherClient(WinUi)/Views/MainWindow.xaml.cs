using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WeatherClient.ViewModels;
using System;

namespace WeatherClient.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        private bool _firstActivationDone = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Получаем диспетчер окна (UI-поток)
            DispatcherQueue? dq = this.DispatcherQueue;

            // Передаём диспетчер в ViewModel
            ViewModel = new MainViewModel(dq);

            // Привязываем ViewModel к корневому элементу содержимого
            if (this.Content is FrameworkElement root)
            {
                root.DataContext = ViewModel;
            }

            // Загружаем данные после первой активации окна (UI уже существует)
            this.Activated += OnWindowActivated;
        }

        private async void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (_firstActivationDone)
                return;

            _firstActivationDone = true;
            this.Activated -= OnWindowActivated;

            try
            {
                await ViewModel.GetWeatherAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки погоды: {ex.Message}");
            }
        }
    }
}
