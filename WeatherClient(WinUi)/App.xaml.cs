using Microsoft.UI.Xaml;
using WeatherClient.Views;

namespace WeatherClient
{
    public partial class App : Application
    {
        private Window m_window;

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        // Логирование необработанных исключений
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"Критическая необработанная ошибка: {e.Message}");
            System.Diagnostics.Debug.WriteLine($"Источник: {e.Exception.Source}");
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new Views.MainWindow();
            m_window.Activate();
        }
    }
}