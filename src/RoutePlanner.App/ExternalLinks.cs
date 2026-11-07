using System.Diagnostics;
using RoutePlanner.Core;

namespace RoutePlanner.App;

internal static class ExternalLinks
{
    // The OS opens one of two fixed HTTPS pages. No executable/path or imported text is accepted.
    public static void Open(string address)
    {
        if (!FortniteGgSite.IsExternalLink(address)) throw new ArgumentException("Unbekanntes Linkziel.");
        Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
    }
}
