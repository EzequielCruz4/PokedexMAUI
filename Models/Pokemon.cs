using SQLite;

namespace PokedexMAUI.Models;

[Table("Pokemon")]
public class Pokemon
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [NotNull, MaxLength(60)]
    public string Nome { get; set; } = string.Empty;
    [NotNull, MaxLength(30)]
    public string Tipo { get; set; } = string.Empty;
    [NotNull]
    public byte[] Imagem { get; set; } = [];
    [Ignore]
    public List<Poder> Poderes { get; set; } = [];
    [Ignore]
    public string Codigo => $"#{Id:D3}";
    [Ignore]
    public string ResumoPoderes => string.Join(" • ", Poderes.Select(p => p.Nome));
}
