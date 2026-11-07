namespace RoutePlanner.Core;

// Ordinary website navigation only: no undocumented API, dataset copying or native bridge.
public static class FortniteGgSite
{
    public const string MapUrl = "https://fortnite.gg/";
    public const string RuntimeUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";
    public static bool IsSiteNavigation(string address) => Uri.TryCreate(address, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.IdnHost == "fortnite.gg" && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo);
    public static bool IsExternalLink(string address) => address == MapUrl || address == RuntimeUrl;
}
