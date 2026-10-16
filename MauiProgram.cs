using Microsoft.Extensions.DependencyInjection;
using PokedexMAUI.Data;

namespace PokedexMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddSingleton(_ => new PokemonDatabase(
            Path.Combine(FileSystem.AppDataDirectory, "pokedex.db3")));

        return builder.Build();
    }
}
