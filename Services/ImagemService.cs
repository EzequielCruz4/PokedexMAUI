using PokedexMAUI.Data;

namespace PokedexMAUI.Services;

public static class ImagemService
{
    public static async Task<byte[]?> SelecionarAsync()
    {
        var resultado = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Escolha a imagem do seu Pokémon ou coisa kk",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = ["image/png", "image/jpeg"],
                [DevicePlatform.WinUI] = [".png", ".jpg", ".jpeg"]
            })
        });
        if (resultado is null)
            return null;

        using var arquivo = await resultado.OpenReadAsync();
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int lidos;
        while ((lidos = await arquivo.ReadAsync(buffer)) > 0)
        {
            if (memoria.Length + lidos > PokemonDatabase.LimiteImagemBytes)
                throw new ArgumentException("Escolha uma imagem com até 5 MB.");
            await memoria.WriteAsync(buffer.AsMemory(0, lidos));
        }

        var bytes = memoria.ToArray();
        var png = bytes.Length >= 8 && bytes.AsSpan(0, 8)
            .SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        if (!png && !jpeg)
            throw new ArgumentException("Escolha um arquivo PNG ou JPG válido.");
        return bytes;
    }
}
