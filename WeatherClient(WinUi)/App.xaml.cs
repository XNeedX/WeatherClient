using Microsoft.UI.Xaml;
using WeatherClient.Models;
using WeatherClient.Services;
using System.Globalization;

namespace WeatherClient
{
    public partial class App : Application
    {
        private Window m_window;
        public Window MainWindow => m_window;

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"=== CRITICAL ERROR ===");
            System.Diagnostics.Debug.WriteLine($"Message: {e.Message}");
            System.Diagnostics.Debug.WriteLine($"Source: {e.Exception?.Source}");
            System.Diagnostics.Debug.WriteLine($"Stack: {e.Exception?.StackTrace}");
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new Views.MainWindow();

            ThemeService.Initialize(m_window);

            ThemeService.ApplyTheme(SettingsService.AppTheme);

            m_window.Activate();
        }
    }
}