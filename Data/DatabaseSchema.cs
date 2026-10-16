namespace PokedexMAUI.Data;

public static class DatabaseSchema
{
    public const string CriarPokemon = """
        CREATE TABLE IF NOT EXISTS Pokemon (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nome TEXT NOT NULL CHECK (length(trim(Nome)) BETWEEN 1 AND 60),
            Tipo TEXT NOT NULL CHECK (length(trim(Tipo)) BETWEEN 1 AND 30),
            Imagem BLOB NOT NULL
        );
        """;
    public const string CriarPoder = """
        CREATE TABLE IF NOT EXISTS Poder (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PokemonId INTEGER NOT NULL,
            Nome TEXT COLLATE NOCASE NOT NULL CHECK (length(trim(Nome)) BETWEEN 1 AND 60),
            FOREIGN KEY (PokemonId) REFERENCES Pokemon(Id) ON DELETE CASCADE,
            UNIQUE (PokemonId, Nome)
        );
        """;
}
