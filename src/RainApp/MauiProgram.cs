using CommunityToolkit.Maui;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RainApp.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace RainApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true)
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Domain (pure Session module behind ports; ADR-0003).
		builder.Services.AddSingleton<IClock, SystemClock>();
		builder.Services.AddSingleton(_ => AppScenes.CreateCatalog());
		builder.Services.AddSingleton<MediaElementAudioOutput>();
		builder.Services.AddSingleton<IAudioOutput>(sp => sp.GetRequiredService<MediaElementAudioOutput>());
		builder.Services.AddSingleton<Session>();

		// MVVM shell.
		builder.Services.AddSingleton<RainViewModel>();
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
