using System.Collections.ObjectModel;
using PokedexMAUI.Data;
using PokedexMAUI.Models;
using PokedexMAUI.Services;

namespace PokedexMAUI.Views;

public partial class CadastroPokemonPage : ContentPage
{
    private readonly PokemonDatabase _banco;
    private readonly int _pokemonId;
    private bool _carregado;
    private bool _ocupado;
    private byte[] _imagemAtual = [];

    public ObservableCollection<Poder> Poderes { get; } = [];
    public IReadOnlyList<string> Tipos => TiposPokemon.Todos;
    public bool Editando => _pokemonId != 0;
    public string TituloTela => Editando ? "Ficha do Pokémon" : "Novo Pokémon";
    public bool TemImagem => ImagemAtual.Length > 0;
    public byte[] ImagemAtual
    {
        get => _imagemAtual;
        private set
        {
            _imagemAtual = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TemImagem));
        }
    }
    public bool Ocupado
    {
        get => _ocupado;
        private set { _ocupado = value; OnPropertyChanged(); }
    }

    public CadastroPokemonPage(PokemonDatabase banco, int pokemonId = 0)
    {
        InitializeComponent();
        _banco = banco;
        _pokemonId = pokemonId;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_carregado || Ocupado) return;
        Ocupado = true;
        try
        {
            if (Editando)
            {
                var pokemon = await _banco.ObterAsync(_pokemonId);
                if (pokemon is null)
                    throw new InvalidOperationException("Esse Pokémon não está mais cadastrado.");
                NomeEntry.Text = pokemon.Nome;
                TipoPicker.SelectedItem = pokemon.Tipo;
                ImagemAtual = pokemon.Imagem;
                foreach (var poder in pokemon.Poderes) Poderes.Add(poder);
            }
            _carregado = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível abrir a ficha", "Volte à coleção e tente novamente.", "OK");
            await Navigation.PopAsync();
        }
        finally { Ocupado = false; }
    }

    private string? AdicionarPoderPendente()
    {
        var nome = PoderEntry.Text?.Trim() ?? string.Empty;
        if (nome.Length == 0) return "Digite o nome do poder antes de adicionar.";
        if (nome.Length > 60) return "O poder deve ter até 60 caracteres.";
        if (Poderes.Any(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            return "Esse poder já está na lista.";
        Poderes.Add(new Poder { Nome = nome });
        PoderEntry.Text = string.Empty;
        return null;
    }

    private async void OnAdicionarPoderClicked(object? sender, EventArgs e)
    {
        if (Ocupado) return;
        if (AdicionarPoderPendente() is string erro)
            await DisplayAlertAsync("Confira o poder", erro, "OK");
    }

    private void OnRemoverPoderClicked(object? sender, EventArgs e)
    {
        if (!Ocupado && sender is Button { CommandParameter: Poder poder })
            Poderes.Remove(poder);
    }

    private async void OnImagemClicked(object? sender, EventArgs e)
    {
        if (Ocupado) return;
        Ocupado = true;
        try
        {
            var bytes = await ImagemService.SelecionarAsync();
            if (bytes is not null) ImagemAtual = bytes;
        }
        catch (ArgumentException ex)
        {
            await DisplayAlertAsync("Confira a imagem", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível abrir a imagem", "Escolha outro arquivo PNG ou JPG.", "OK");
        }
        finally { Ocupado = false; }
    }

    private void OnRemoverImagemClicked(object? sender, EventArgs e)
    {
        if (!Ocupado) ImagemAtual = [];
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        if (Ocupado || !_carregado) return;
        if (!string.IsNullOrWhiteSpace(PoderEntry.Text) && AdicionarPoderPendente() is string erro)
        {
            await DisplayAlertAsync("Confira o poder", erro, "OK");
            return;
        }

        var pokemon = new Pokemon
        {
            Id = _pokemonId,
            Nome = NomeEntry.Text ?? string.Empty,
            Tipo = TipoPicker.SelectedItem as string ?? string.Empty,
            Imagem = ImagemAtual,
            Poderes = Poderes.Select(p => new Poder { Nome = p.Nome }).ToList()
        };

        Ocupado = true;
        try
        {
            await _banco.SalvarAsync(pokemon);
            await Navigation.PopAsync();
        }
        catch (ArgumentException ex)
        {
            await DisplayAlertAsync("Confira o cadastro", ex.Message, "OK");
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Confira o cadastro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível salvar", "O cadastro continua nesta tela. Tente salvar novamente.", "OK");
        }
        finally { Ocupado = false; }
    }

    private async void OnExcluirClicked(object? sender, EventArgs e)
    {
        if (Ocupado || !Editando) return;
        var confirmar = await DisplayAlertAsync("Excluir Pokémon?",
            "O Pokémon e seus poderes serão removidos da coleção.", "Excluir", "Cancelar");
        if (!confirmar || Ocupado) return;
        Ocupado = true;
        try
        {
            await _banco.ExcluirAsync(_pokemonId);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível excluir", "Tente novamente.", "OK");
        }
        finally { Ocupado = false; }
    }

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        if (!Ocupado) await Navigation.PopAsync();
    }
}
