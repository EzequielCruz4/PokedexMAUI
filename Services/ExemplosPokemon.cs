using PokedexMAUI.Models;

namespace PokedexMAUI.Services;

public static class ExemplosPokemon
{
    public static async Task<List<Pokemon>> CriarAsync()
    {
        var exemplos = new[]
        {
            (Nome: "Bulbasaur", Tipo: "Planta", Arquivo: "bulbasaur", Poderes: new[] { "Chicote de Vinha", "Folha Navalha", "Raio Solar" }),
            (Nome: "Charmander", Tipo: "Fogo", Arquivo: "charmander", Poderes: new[] { "Brasa", "Lança-chamas", "Garra de Dragão" }),
            (Nome: "Squirtle", Tipo: "Água", Arquivo: "squirtle", Poderes: new[] { "Jato d'Água", "Bolhas", "Hidrobomba" }),
        };

        var pokemons = new List<Pokemon>();
        foreach (var exemplo in exemplos)
        {
            using var arquivo = await FileSystem.OpenAppPackageFileAsync($"exemplos/{exemplo.Arquivo}.png");
            using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria);
            pokemons.Add(new Pokemon
            {
                Nome = exemplo.Nome,
                Tipo = exemplo.Tipo,
                Imagem = memoria.ToArray(),
                Poderes = exemplo.Poderes.Select(p => new Poder { Nome = p }).ToList()
            });
        }
        return pokemons;
    }
}
