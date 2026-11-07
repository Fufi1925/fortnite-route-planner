using System.Net.Http;
using System.Text.Json;

namespace RoutePlanner.Core;

public sealed record PublicMapSnapshot(byte[] ImageBytes, DateTimeOffset RetrievedAtUtc, DateTimeOffset? ImageModifiedAtUtc);

// Public, static map artwork only. No game process, account, private protocol, or match-state access.
public sealed class PublicFortniteMapClient : IDisposable
{
    private readonly HttpClient client;
    private DateTimeOffset retryNotBefore;
    public PublicFortniteMapClient(HttpMessageHandler? testHandler = null)
    {
        client = new(testHandler ?? new HttpClientHandler { AllowAutoRedirect = false, UseDefaultCredentials = false });
        client.Timeout = TimeSpan.FromSeconds(25);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RoutePlanner/0.4.0");
    }
    public async Task<PublicMapSnapshot> DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.UtcNow < retryNotBefore) throw new IOException($"Kartenanbieter begrenzt Anfragen bis {retryNotBefore.ToLocalTime():HH:mm}. Bitte später erneut versuchen.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        var token = deadline.Token;
        try
        {
            using var api = await client.GetAsync(PublicMapContract.ApiUrl,HttpCompletionOption.ResponseHeadersRead,token);
            RequireSuccess(api);
            var json = await ReadBounded(api,2*1024*1024,token);
            using var doc = JsonDocument.Parse(json,new JsonDocumentOptions { MaxDepth = 16 });
            var root = doc.RootElement;
            if (root.GetProperty("status").GetInt32() != 200 || root.GetProperty("data").GetProperty("images").GetProperty("blank").GetString() != PublicMapContract.ImageUrl)
                throw new InvalidDataException("Kartenanbieter hat ein unbekanntes Bildziel geliefert. Es wird nichts von fremden Adressen geladen.");
            using var image = await client.GetAsync(PublicMapContract.ImageUrl,HttpCompletionOption.ResponseHeadersRead,token);
            RequireSuccess(image);
            var bytes = await ReadBounded(image,20*1024*1024,token);
            if (bytes.Length < 8 || !bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
                throw new InvalidDataException("Kartenanbieter hat kein gültiges PNG-Bild geliefert.");
            return new(bytes,DateTimeOffset.UtcNow,image.Content.Headers.LastModified);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new IOException("Kartenabruf hat das Zeitlimit überschritten. Die lokale Karte bleibt erhalten."); }
        catch (HttpRequestException ex) { throw new IOException("Kartenanbieter ist nicht erreichbar. Die lokale Karte bleibt erhalten.",ex); }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new InvalidDataException("Antwort des Kartenanbieters hat ein unbekanntes Format.",ex); }
    }
    private void RequireSuccess(HttpResponseMessage response)
    {
        if ((int)response.StatusCode == 429)
        {
            var after = response.Headers.RetryAfter;
            retryNotBefore = after?.Date ?? DateTimeOffset.UtcNow+(after?.Delta ?? TimeSpan.FromMinutes(1));
            throw new IOException("Kartenanbieter begrenzt Anfragen. Bitte später erneut versuchen; kein automatischer Wiederholungsversuch.");
        }
        if (!response.IsSuccessStatusCode) throw new IOException($"Kartenabruf fehlgeschlagen (HTTP {(int)response.StatusCode}). Keine automatische Weiterleitung oder Wiederholung.");
    }
    private static async Task<byte[]> ReadBounded(HttpResponseMessage response,int maximum,CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength > maximum) throw new InvalidDataException("Kartenantwort überschreitet die erlaubte Dateigröße.");
        await using var input = await response.Content.ReadAsStreamAsync(ct);
        using var output = new MemoryStream(); byte[] buffer = new byte[8192]; int count;
        while ((count = await input.ReadAsync(buffer,ct)) > 0)
        {
            if (output.Length+count > maximum) throw new InvalidDataException("Kartenantwort überschreitet die erlaubte Dateigröße.");
            output.Write(buffer,0,count);
        }
        return output.ToArray();
    }
    public void Dispose() => client.Dispose();
}
