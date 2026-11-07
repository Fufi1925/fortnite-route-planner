using System.Diagnostics;
using RoutePlanner.Core;

namespace RoutePlanner.App;

internal static class ExternalLinks
{
    // The OS opens a fixed HTTPS page or Epic's fixed Fortnite launch URI. No imported target/path.
    public static void Open(string address)
    {
        if (!FortniteGgSite.IsExternalLink(address) && address != EpicLaunchContract.FortniteUri) throw new ArgumentException("Unbekanntes Linkziel.");
        Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
    }
}
