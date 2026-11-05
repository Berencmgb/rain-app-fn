using Microsoft.Extensions.DependencyInjection;

namespace RainApp;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		_services = services;
		// App resources (design tokens in Resources/Styles) must exist before
		// any page resolves StaticResource keys, so MainPage is resolved in
		// CreateWindow — not injected into this constructor.
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_services.GetRequiredService<MainPage>());
	}
}
