using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Slider.Heuristics;
using Slider.Interfaces;
using Slider.Solver;
using Slider.ViewModels;
using System.Configuration;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Windows;

namespace Slider
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;
        private readonly string _applicationDisplayName = "Sliding Puzzle";
        public App()
        {
            ConfigurationBuilder builder = new();
            IConfigurationRoot _configurationRoot = builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<IMainViewModel, MainViewModel>();
                    services.AddSingleton<IUserAlert>(serviceProvider => serviceProvider.GetRequiredService<MainWindow>());
                    services.AddSingleton<IModel, Model>();
                    services.AddSingleton<IGenerator, PuzzleGenerator>();
                    services.AddSingleton<IOptions, Options>();
                    services.AddSingleton<ITileControlViewModelFactory, TileControlViewModelFactory>();
                    services.AddTransient<ISolver, WeightedAStarSolver>();
                    services.AddSingleton<IHeuristicElementFactory, HeuristicElementFactory>();
                })
                .Build();
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            //Global unhandled exception handler:
            Application.Current.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(args.Exception.Message, _applicationDisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                _host.Start();
                MainWindow startupForm = _host.Services.GetRequiredService<MainWindow>();
                startupForm.DataContext = _host.Services.GetRequiredService<IMainViewModel>();
                startupForm.Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _applicationDisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                Task t = _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
