using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using RainApp.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace RainApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		// Platform audio-event observers and remote-control targets (ticket #7) attach
		// once the platform app has launched; the provider they resolve through is the
		// one built below — lifecycle events only run after this method returns.
		IServiceProvider? services = null;
		builder.ConfigureLifecycleEvents(events =>
		{
#if IOS
			events.AddiOS(ios => ios.FinishedLaunching((_, _) =>
			{
				if (services is { } ready) PlatformPlaybackControls.Register(ready);
				return false;
			}));
#endif
#if ANDROID
			events.AddAndroid(android => android.OnCreate((activity, _) =>
			{
				if (services is { } ready) PlatformPlaybackControls.Register(activity, ready);
			}));
#endif
		});

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
		builder.Services.AddSingleton<IPersistence, PreferencesPersistence>();
		builder.Services.AddSingleton(_ => AppScenes.CreateCatalog());
		builder.Services.AddSingleton<MediaElementAudioOutput>();
		builder.Services.AddSingleton<IAudioOutput>(sp => sp.GetRequiredService<MediaElementAudioOutput>());
		builder.Services.AddSingleton<Session>();
		builder.Services.AddSingleton<PlatformEventBridge>();

		// MVVM shell.
		builder.Services.AddSingleton<RainViewModel>();
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Platform events → Session intents: constructing the bridge is the wiring,
		// and it must exist before the first platform event can arrive.
		app.Services.GetRequiredService<PlatformEventBridge>();

		services = app.Services;
		return app;
	}
}
