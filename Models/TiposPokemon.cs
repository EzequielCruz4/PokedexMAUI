namespace PokedexMAUI.Models;

public static class TiposPokemon
{
    public static IReadOnlyList<string> Todos { get; } = Array.AsReadOnly(new[]
    {
        "Água", "Aço", "Dragão", "Elétrico", "Fada", "Fantasma", "Fogo",
        "Gelo", "Inseto", "Lutador", "Normal", "Pedra", "Planta",
        "Psíquico", "Sombrio", "Terra", "Venenoso", "Voador", "DIFERENCIADO"
    });
}
