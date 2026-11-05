using CommunityToolkit.Maui.Views;
using RainApp.Services;

namespace RainApp;

public partial class MainPage : ContentPage
{
	public MainPage(RainViewModel viewModel, MediaElementAudioOutput audio)
	{
		InitializeComponent();
		// The single long-lived player lives in this page's tree (ADR-0001);
		// the adapter drives it from Session intents.
		audio.Attach(Player);
		BindingContext = viewModel;
	}
}
