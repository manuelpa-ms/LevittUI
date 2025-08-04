using Microsoft.Extensions.Logging;
using LevittUI.Services;
using LevittUI.ViewModels;
using LevittUI.Views;

namespace LevittUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register HTTP client with configuration
		builder.Services.AddHttpClient<IHomeAutomationService, HomeAutomationService>(client =>
		{
			client.Timeout = TimeSpan.FromSeconds(30);
		})
		.ConfigurePrimaryHttpMessageHandler(() =>
		{
			var handler = new HttpClientHandler();
			// Allow invalid certificates for development (if needed)
			handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
			return handler;
		});

		// Register additional HTTP client for general use
		builder.Services.AddHttpClient();

		// Register services
		builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
		builder.Services.AddSingleton<IHomeAutomationService, HomeAutomationService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<MainViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<MainPage>();

		// Add logging for both debug and release builds
#if DEBUG
		builder.Logging.AddDebug();
#else
		// Add debug logging for release builds to help with debugging
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

		return builder.Build();
	}
}
