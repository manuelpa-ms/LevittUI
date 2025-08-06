using Microsoft.Extensions.Logging;
using LevittUI.Services;
using LevittUI.ViewModels;
using LevittUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace LevittUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        System.Diagnostics.Debug.WriteLine("[MauiProgram] Starting CreateMauiApp...");
        
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

		// Configure HTTP handler for proxy support
		Func<HttpMessageHandler> configureHandler = () =>
		{
			var handler = new HttpClientHandler();
			
			// Log handler creation for debugging
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** Handler Factory Called! Creating HttpClientHandler...");
			
			// Disable automatic cookie handling to prevent versioned cookie format
			handler.UseCookies = false;
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** Disabled automatic cookie handling to use simple format");
			
#if DEBUG && ANDROID
			// Configure Fiddler proxy for Android debugging
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** DEBUG && ANDROID - Configuring Fiddler proxy...");
			handler.Proxy = new System.Net.WebProxy("192.168.1.63:8888", false);
			handler.UseProxy = true;
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** Proxy configured: 192.168.1.63:8888");
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** UseProxy: {handler.UseProxy}");
#else
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** Not Android DEBUG - no proxy configured");
#endif
			
			// Allow invalid certificates for development (if needed)
			handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => 
			{
				System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** Certificate validation callback called for: {message?.RequestUri}");
				return true;
			};
			
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] *** HttpClientHandler created successfully");
			return handler;
		};        // Register HTTP client with configuration for HomeAutomationService as SINGLETON
        System.Diagnostics.Debug.WriteLine("[MauiProgram] *** Registering HomeAutomationService with HttpClient...");
        
        // First, configure the HttpClient
        builder.Services.AddHttpClient<HomeAutomationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            System.Diagnostics.Debug.WriteLine("[MauiProgram] *** Configuring HomeAutomationService HttpClient timeout to 30 seconds");
        })
        .ConfigurePrimaryHttpMessageHandler(configureHandler);
        
        // Then register as singleton using factory to preserve HttpClient injection
        builder.Services.AddSingleton<IHomeAutomationService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(HomeAutomationService));
            var logger = serviceProvider.GetRequiredService<ILogger<HomeAutomationService>>();
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            return new HomeAutomationService(httpClient, logger, configService);
        });

        // Register additional HTTP client for general use with same proxy configuration
        System.Diagnostics.Debug.WriteLine("[MauiProgram] *** Registering default HttpClient...");
        builder.Services.AddHttpClient("default", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(configureHandler);

        // Register services
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();

        // Register Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();

        // Add logging for both debug and release builds
#if DEBUG
        builder.Logging.AddDebug();
        System.Diagnostics.Debug.WriteLine("[MauiProgram] *** Debug logging enabled");
#else
        // Add debug logging for release builds to help with debugging
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

        System.Diagnostics.Debug.WriteLine("[MauiProgram] *** MauiApp build complete");
        return builder.Build();
    }
}
