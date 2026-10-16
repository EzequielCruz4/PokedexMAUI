using PokedexMAUI.Data;
using PokedexMAUI.Services;

namespace PokedexMAUI.Views;

public partial class BancoPage : ContentPage
{
    private readonly PokemonDatabase _banco;
    private bool _ocupado;

    public string CaminhoBanco => _banco.CaminhoBanco;
    public string Resumo { get; private set; } = "Carregando...";
    public bool Ocupado
    {
        get => _ocupado;
        private set { _ocupado = value; OnPropertyChanged(); }
    }

    public BancoPage(PokemonDatabase banco)
    {
        InitializeComponent();
        _banco = banco;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Ocupado) return;
        Ocupado = true;
        try
        {
            var totais = await _banco.ContarAsync();
            Resumo = $"{totais.Pokemons} Pokémon e {totais.Poderes} poderes cadastrados";
            OnPropertyChanged(nameof(Resumo));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível consultar o banco", "Volte à coleção e tente novamente.", "OK");
        }
        finally { Ocupado = false; }
    }

    private async void OnCopiarClicked(object? sender, EventArgs e)
    {
        if (Ocupado) return;
        try
        {
            await Clipboard.Default.SetTextAsync(CaminhoBanco);
            await DisplayAlertAsync("Caminho copiado", "O caminho do banco está na área de transferência.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível copiar", "Tente novamente.", "OK");
        }
    }

    private async void OnExportarClicked(object? sender, EventArgs e)
    {
        if (Ocupado) return;
        Ocupado = true;
        try
        {
            var copia = Path.Combine(FileSystem.CacheDirectory, $"pokedex-{Guid.NewGuid():N}.db3");
            await _banco.CriarCopiaAsync(copia);
            var exportado = await ExportacaoService.SalvarOuCompartilharAsync(copia);
#if WINDOWS
            if (exportado)
                await DisplayAlertAsync("Cópia salva", "Você pode abrir o arquivo em um visualizador de SQLite.", "OK");
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível exportar", "A coleção continua salva no aplicativo. Tente novamente.", "OK");
        }
        finally { Ocupado = false; }
    }
}
