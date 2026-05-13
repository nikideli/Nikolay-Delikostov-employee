using System.Windows;
using System.Windows.Threading;
using EmployeePairFinder.Infrastructure;
using EmployeePairFinder.Services;
using EmployeePairFinder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeePairFinder;

// Composition root: builds the DI container and manages the application lifetime.
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IDialogService, DialogService>();

        // Domain services — stateless, safe as singletons
        services.AddSingleton<IDateParserService,    DateParserService>();
        services.AddSingleton<IFileValidatorService, FileValidatorService>();
        services.AddSingleton<ICsvParserService,     CsvParserService>();
        services.AddSingleton<IEmployeePairService,  EmployeePairService>();

        // ViewModel — transient so it gets a clean state every time it is resolved
        services.AddTransient<MainViewModel>();

        // Main window — singleton because we only ever show one instance
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Install global handler BEFORE showing the window so nothing slips through
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Resolve and show the main window via the DI container
        var mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose the container so IDisposable services are cleaned up correctly
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    // ── Global exception handler ──────────────────────────────────────────────

    // Last-resort handler: logs, shows a dialog, and keeps the app alive.
    private static void OnDispatcherUnhandledException(
        object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = $"An unexpected error occurred:\n\n{e.Exception.Message}";
        Console.WriteLine($"[FATAL] Unhandled dispatcher exception: {e.Exception}");

        MessageBox.Show(
            message,
            "Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Mark as handled to prevent the default "application crashed" dialog
        e.Handled = true;
    }
}

