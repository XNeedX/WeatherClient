using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using WeatherClient.Views;

namespace WeatherClient
{
    public partial class App : Application
    {
        private Window m_window; // <-- поле m_window было у вас объявлено

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // (Ваш код обработки исключений)
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Установите точку останова ПРЯМО ЗДЕСЬ.
            m_window = new Views.MainWindow(); // <-- Если не останавливается, проблема тут
            m_window.Activate();
        }
    }
}
