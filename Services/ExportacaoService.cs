namespace PokedexMAUI.Services;

public static class ExportacaoService
{
    public static async Task<bool> SalvarOuCompartilharAsync(string caminho)
    {
#if WINDOWS
        var janela = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView
            as Microsoft.UI.Xaml.Window
            ?? throw new InvalidOperationException("Não foi possível localizar a janela do aplicativo.");

        var seletor = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = "pokedex",
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        seletor.FileTypeChoices.Add("Banco de dados SQLite", new List<string> { ".db3" });
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(janela);
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, handle);
        var destino = await seletor.PickSaveFileAsync();
        if (destino is null)
            return false;
        await Task.Run(() => File.Copy(caminho, destino.Path, overwrite: true));
#else
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Cópia do banco da Pokédex",
            File = new ShareFile(caminho, "application/octet-stream")
        });
#endif
        return true;
    }
}
