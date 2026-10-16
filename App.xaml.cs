using PokedexMAUI.Views;
using PokedexMAUI.Data;

namespace PokedexMAUI;

public partial class App : Application
{
    private readonly PokemonDatabase _banco;

    public App(PokemonDatabase banco)
    {
        InitializeComponent();
        _banco = banco;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new ListaPokemonPage(_banco))
        {
            BarBackgroundColor = Color.FromArgb("#FFFFFF"),
            BarTextColor = Color.FromArgb("#172238")
        });

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            window.Width = 1050;
            window.Height = 820;
            window.MinimumWidth = 390;
            window.MinimumHeight = 600;
        }

        return window;
    }
}
