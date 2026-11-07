using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RoutePlanner.Core;

namespace RoutePlanner.App;

public sealed class MapImages(string directory)
{
    private const int MaxInputBytes = 20 * 1024 * 1024;
    private readonly string imageDirectory = Path.Combine(directory, "maps");
    public (MapAsset Asset, BitmapSource Image) Import(string path)
    {
        using var input = File.OpenRead(path);
        if (input.Length > MaxInputBytes) throw new InvalidDataException("Kartenbild maximal 20 MiB.");
        byte[] header = new byte[8]; input.ReadExactly(header); input.Position = 0;
        bool png = header.SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        bool jpeg = header[0] == 255 && header[1] == 216 && header[2] == 255;
        if (!png && !jpeg) throw new InvalidDataException("Bitte eine PNG- oder JPEG-Karte öffnen.");
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        int width = decoder.Frames[0].PixelWidth, height = decoder.Frames[0].PixelHeight;
        if (width != height || width < 64 || width > 16000)
            throw new InvalidDataException("Die Karte muss quadratisch sein (64…16.000 Pixel). Sonst stimmen die Abstände nicht.");
        input.Position = 0;
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = input; bitmap.DecodePixelWidth = Math.Min(width, 2048); bitmap.EndInit(); bitmap.Freeze();
        var normalized = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0); normalized.Freeze();
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(normalized));
        using var output = new MemoryStream(); encoder.Save(output); byte[] bytes = output.ToArray();
        string id = "MAP-" + Convert.ToHexStringLower(SHA256.HashData(bytes));
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.Length > 120) name = name[..120];
        var asset = new MapAsset { Id = id, Name = name, Source = "Eigene Datei: " + name };
        RoutePlanner.Core.Validation.Map(asset);
        RouteJson.WriteAtomic(ImagePath(asset), bytes);
        return (asset, normalized);
    }
    public BitmapSource Load(MapAsset asset)
    {
        RoutePlanner.Core.Validation.Map(asset);
        var path = ImagePath(asset);
        using var file = File.OpenRead(path);
        if (file.Length > MaxInputBytes) throw new InvalidDataException("Gespeichertes Kartenbild zu groß.");
        byte[] bytes = new byte[file.Length]; file.ReadExactly(bytes);
        if ("MAP-" + Convert.ToHexStringLower(SHA256.HashData(bytes)) != asset.Id)
            throw new InvalidDataException("Kartenbild stimmt nicht mit seiner Datenkennung überein.");
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(bytes); image.DecodePixelWidth = 2048; image.EndInit(); image.Freeze();
        return image;
    }
    private string ImagePath(MapAsset asset)
    {
        RoutePlanner.Core.Validation.Map(asset);
        if (!asset.HasImage) throw new InvalidDataException("Arbeitsfläche hat kein Kartenbild.");
        return Path.Combine(imageDirectory, asset.Id + ".png");
    }
    public void DeleteKnown(IEnumerable<MapAsset> assets)
    {
        foreach (var asset in assets.Where(a => a.HasImage))
        { string path = ImagePath(asset); if (File.Exists(path)) File.Delete(path); }
    }
}
