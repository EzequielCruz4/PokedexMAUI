using System.Collections.ObjectModel;
using System.Globalization;
using PokedexMAUI.Data;
using PokedexMAUI.Models;
using PokedexMAUI.Services;

namespace PokedexMAUI.Views;

public partial class ListaPokemonPage : ContentPage
{
    private readonly PokemonDatabase _banco;
    private List<Pokemon> _todos = [];
    private bool _navegando;
    private bool _ocupado;

    public ObservableCollection<Pokemon> Pokemons { get; } = [];
    public List<string> TiposFiltro { get; } = ["Todos os tipos", .. TiposPokemon.Todos];
    public string ContagemTexto { get; private set; } = "Carregando coleção...";
    public bool ColecaoVazia => _todos.Count == 0;
    public string MensagemVazia => ColecaoVazia ? "Sua aventura começa aqui" : "Nenhum Pokémon encontrado";
    public string AjudaVazia => ColecaoVazia ? "Cadastre seu primeiro Pokémon ou experimente os exemplos."
        : "Tente outro nome, poder ou tipo.";
    public bool Ocupado
    {
        get => _ocupado;
        private set { _ocupado = value; OnPropertyChanged(); }
    }

    public ListaPokemonPage(PokemonDatabase banco)
    {
        InitializeComponent();
        _banco = banco;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        if (Ocupado) return;
        Ocupado = true;
        try
        {
            _todos = await _banco.ListarAsync();
            Filtrar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ContagemTexto = "Não foi possível carregar a coleção.";
            OnPropertyChanged(nameof(ContagemTexto));
            await DisplayAlertAsync("Não foi possível abrir a coleção",
                "Feche e abra o aplicativo para tentar novamente.", "OK");
        }
        finally { Ocupado = false; }
    }

    private void Filtrar()
    {
        if (BuscaBar is null || TipoFiltroPicker is null) return;
        var busca = BuscaBar.Text?.Trim() ?? string.Empty;
        var tipo = TipoFiltroPicker.SelectedItem as string ?? "Todos os tipos";
        var resultados = _todos.Where(p =>
            (tipo == "Todos os tipos" || p.Tipo == tipo) &&
            (Contem(p.Nome, busca) || p.Poderes.Any(poder => Contem(poder.Nome, busca)))).ToList();

        Pokemons.Clear();
        foreach (var pokemon in resultados) Pokemons.Add(pokemon);
        ContagemTexto = resultados.Count == _todos.Count
            ? $"{_todos.Count} Pokémon na coleção"
            : $"{resultados.Count} de {_todos.Count} Pokémon";
        OnPropertyChanged(nameof(ContagemTexto));
        OnPropertyChanged(nameof(ColecaoVazia));
        OnPropertyChanged(nameof(MensagemVazia));
        OnPropertyChanged(nameof(AjudaVazia));
    }

    private static bool Contem(string texto, string busca) =>
        CultureInfo.CurrentCulture.CompareInfo.IndexOf(texto, busca,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private void OnBuscaAlterada(object? sender, TextChangedEventArgs e) => Filtrar();
    private void OnFiltroAlterado(object? sender, EventArgs e) => Filtrar();

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (PokemonCollection?.ItemsLayout is GridItemsLayout layout)
            layout.Span = width >= 820 ? 2 : 1;
    }

    private async Task NavegarAsync(Page pagina)
    {
        if (_navegando || Ocupado) return;
        _navegando = true;
        try { await Navigation.PushAsync(pagina); }
        finally { _navegando = false; }
    }

    private async void OnNovoClicked(object? sender, EventArgs e)
        => await NavegarAsync(new CadastroPokemonPage(_banco));

    private async void OnBancoClicked(object? sender, EventArgs e)
        => await NavegarAsync(new BancoPage(_banco));

    private async void OnPokemonSelecionado(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Pokemon pokemon) return;
        PokemonCollection.SelectedItem = null;
        await NavegarAsync(new CadastroPokemonPage(_banco, pokemon.Id));
    }

    private async void OnExemplosClicked(object? sender, EventArgs e)
    {
        if (Ocupado) return;
        Ocupado = true;
        try
        {
            await _banco.AdicionarExemplosSeVazioAsync(await ExemplosPokemon.CriarAsync());
            BuscaBar.Text = string.Empty;
            TipoFiltroPicker.SelectedIndex = 0;
            _todos = await _banco.ListarAsync();
            Filtrar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível carregar os exemplos",
                "Tente novamente ou cadastre um Pokémon pelo botão + Novo.", "OK");
        }
        finally { Ocupado = false; }
    }
}
