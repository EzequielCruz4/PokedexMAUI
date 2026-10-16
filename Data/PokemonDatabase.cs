using PokedexMAUI.Models;
using SQLite;

namespace PokedexMAUI.Data;

public sealed class PokemonDatabase : IAsyncDisposable
{
    public const int LimiteImagemBytes = 5 * 1024 * 1024;

    private readonly SemaphoreSlim _acesso = new(1, 1);
    private SQLiteConnection? _conexao;
    private bool _descartado;

    public string CaminhoBanco { get; }

    public PokemonDatabase(string caminhoBanco)
    {
        CaminhoBanco = Path.GetFullPath(caminhoBanco);
    }

    // Abertura, criação de tabelas e acesso acontecem fora da thread da interface.
    // O semáforo impede que dois salvamentos se misturem.
    private async Task<T> ExecutarAsync<T>(Func<SQLiteConnection, T> operacao)
    {
        await _acesso.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_descartado, this);
            return await Task.Run(() => operacao(Conectar()));
        }
        finally
        {
            _acesso.Release();
        }
    }

    private SQLiteConnection Conectar()
    {
        if (_conexao is not null)
            return _conexao;

        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoBanco)!);
        var conexao = new SQLiteConnection(CaminhoBanco,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        try
        {
            conexao.Execute("PRAGMA foreign_keys = ON;");
            conexao.Execute(DatabaseSchema.CriarPokemon);
            conexao.Execute(DatabaseSchema.CriarPoder);
            _conexao = conexao;
            return conexao;
        }
        catch
        {
            conexao.Dispose();
            throw;
        }
    }

    public Task<List<Pokemon>> ListarAsync() => ExecutarAsync(db =>
    {
        var pokemons = db.Table<Pokemon>().ToList();
        var poderes = db.Table<Poder>().ToList().ToLookup(p => p.PokemonId);
        foreach (var pokemon in pokemons)
            pokemon.Poderes = poderes[pokemon.Id].OrderBy(p => p.Id).ToList();
        return pokemons.OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    });

    public Task<Pokemon?> ObterAsync(int id) => ExecutarAsync(db =>
    {
        var pokemon = db.Find<Pokemon>(id);
        if (pokemon is not null)
            pokemon.Poderes = db.Table<Poder>().Where(p => p.PokemonId == id)
                .OrderBy(p => p.Id).ToList();
        return pokemon;
    });

    public async Task SalvarAsync(Pokemon pokemon)
    {
        ArgumentNullException.ThrowIfNull(pokemon);
        var registro = PrepararCadastro(pokemon);
        await ExecutarAsync(db =>
        {
            // Cadastro e poderes são uma única transação: tudo salva ou nada salva.
            db.RunInTransaction(() => Gravar(db, registro));
            return true;
        });

        // Só altera o objeto original depois que a transação foi confirmada.
        pokemon.Id = registro.Id;
        pokemon.Nome = registro.Nome;
        pokemon.Tipo = registro.Tipo;
        pokemon.Imagem = registro.Imagem;
        pokemon.Poderes = registro.Poderes;
    }

    private static Pokemon PrepararCadastro(Pokemon pokemon)
    {
        var nome = (pokemon.Nome ?? string.Empty).Trim();
        var tipo = (pokemon.Tipo ?? string.Empty).Trim();
        if (nome.Length is < 1 or > 60)
            throw new ArgumentException("Informe um nome com até 60 caracteres.");
        if (!TiposPokemon.Todos.Contains(tipo))
            throw new ArgumentException("Selecione um tipo de Pokémon.");
        if (pokemon.Id < 0)
            throw new ArgumentException("O identificador do Pokémon é inválido.");
        if ((pokemon.Imagem?.Length ?? 0) > LimiteImagemBytes)
            throw new ArgumentException("A imagem deve ter no máximo 5 MB.");

        var nomesPoderes = (pokemon.Poderes ?? []).Select(p => (p.Nome ?? "").Trim())
            .ToList();
        if (nomesPoderes.Count == 0)
            throw new ArgumentException("Adicione pelo menos um poder.");
        if (nomesPoderes.Any(p => p.Length is < 1 or > 60))
            throw new ArgumentException("Cada poder deve ter entre 1 e 60 caracteres.");

        return new Pokemon
        {
            Id = pokemon.Id,
            Nome = nome,
            Tipo = tipo,
            Imagem = pokemon.Imagem?.ToArray() ?? [],
            Poderes = nomesPoderes.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new Poder { Nome = p }).ToList()
        };
    }

    private static void Gravar(SQLiteConnection db, Pokemon pokemon)
    {
        if (pokemon.Id == 0)
            db.Insert(pokemon);
        else
        {
            if (db.Find<Pokemon>(pokemon.Id) is null)
                throw new InvalidOperationException("Esse Pokémon não está mais cadastrado.");
            db.Update(pokemon);
            db.Execute("DELETE FROM Poder WHERE PokemonId = ?", pokemon.Id);
        }

        foreach (var poder in pokemon.Poderes)
        {
            poder.PokemonId = pokemon.Id;
            db.Insert(poder);
        }
    }

    public Task ExcluirAsync(int id) => ExecutarAsync(db =>
    {
        return db.Delete<Pokemon>(id);
    });

    public Task<(int Pokemons, int Poderes)> ContarAsync() => ExecutarAsync(db =>
        (db.Table<Pokemon>().Count(), db.Table<Poder>().Count()));

    public Task CriarCopiaAsync(string destino) => ExecutarAsync(db =>
    {
        var caminho = Path.GetFullPath(destino);
        if (string.Equals(caminho, CaminhoBanco, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Escolha um caminho diferente do banco em uso.");
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        db.Backup(caminho);
        return true;
    });

    public Task AdicionarExemplosSeVazioAsync(IEnumerable<Pokemon> exemplos)
    {
        var registros = exemplos.Select(PrepararCadastro).ToList();
        foreach (var registro in registros)
            registro.Id = 0;
        return ExecutarAsync(db =>
        {
            db.RunInTransaction(() =>
            {
                if (db.Table<Pokemon>().Count() != 0)
                    throw new InvalidOperationException("Os exemplos só podem ser carregados em uma coleção vazia.");
                foreach (var registro in registros)
                    Gravar(db, registro);
            });
            return true;
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _acesso.WaitAsync();
        try
        {
            _conexao?.Dispose();
            _conexao = null;
            _descartado = true;
        }
        finally
        {
            _acesso.Release();
        }
    }
}
