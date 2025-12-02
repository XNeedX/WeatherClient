using Microsoft.UI.Xaml;

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

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"=== КРИТИЧЕСКАЯ ОШИБКА ===");
            System.Diagnostics.Debug.WriteLine($"Сообщение: {e.Message}");
            System.Diagnostics.Debug.WriteLine($"Источник: {e.Exception?.Source}");
            System.Diagnostics.Debug.WriteLine($"Стек: {e.Exception?.StackTrace}");
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new Views.MainWindow();
            m_window.Activate();
        }
    }
}