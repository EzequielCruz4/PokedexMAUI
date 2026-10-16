using SQLite;

namespace PokedexMAUI.Models;

[Table("Poder")]
public class Poder
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PokemonId { get; set; }

    [NotNull, MaxLength(60)]
    public string Nome { get; set; } = string.Empty;
}
