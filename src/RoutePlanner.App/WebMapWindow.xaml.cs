using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using RoutePlanner.Core;

namespace RoutePlanner.App;

public partial class WebMapWindow : Window
{
    private readonly string profileDirectory;
    private readonly TaskCompletionSource<bool> firstNavigation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closed;
    public Task<bool> InitialNavigation => firstNavigation.Task;
    public WebMapWindow(string dataDirectory)
    {
        profileDirectory = Path.Combine(dataDirectory, "web-fortnite-gg");
        InitializeComponent();
        Loaded += async (_, _) => await InitializeBrowser();
        Closed += (_, _) => { closed = true; firstNavigation.TrySetResult(false); Site.Dispose(); };
    }
    private async Task InitializeBrowser()
    {
        try
        {
            // Separate app profile; no access to the user's normal browser or local route data.
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder:profileDirectory);
            if (closed) return;
            await Site.EnsureCoreWebView2Async(environment);
            if (closed) return;
            var browser = Site.CoreWebView2;
            browser.Settings.AreHostObjectsAllowed = false;
            browser.Settings.IsWebMessageEnabled = false;
            browser.Settings.IsGeneralAutofillEnabled = false;
            browser.Settings.IsPasswordAutosaveEnabled = false;
            browser.Settings.AreDevToolsEnabled = false;
            browser.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            browser.DownloadStarting += (_, e) => { e.Cancel = true; PageStatus.Text = "Downloads bitte über ‚Im Browser‘ ausführen."; };
            browser.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (FortniteGgSite.IsSiteNavigation(e.Uri)) browser.Navigate(e.Uri);
                else PageStatus.Text = "Externer Link: bitte ‚Im Browser‘ verwenden.";
            };
            browser.NavigationStarting += (_, e) =>
            {
                if (!FortniteGgSite.IsSiteNavigation(e.Uri))
                { e.Cancel = true; PageStatus.Text = "Diese Ansicht öffnet ausschließlich https://fortnite.gg/. Für andere Seiten ‚Im Browser‘ nutzen."; }
                else PageStatus.Text = "Fortnite.GG wird geladen …";
            };
            browser.NavigationCompleted += (_, e) =>
            {
                if (closed) return;
                firstNavigation.TrySetResult(e.IsSuccess);
                if (!e.IsSuccess) ShowError("Die Webseite konnte nicht geladen werden (" + e.WebErrorStatus + "). Internetverbindung prüfen oder im Browser öffnen.");
                else { ErrorPanel.Visibility = Visibility.Collapsed; Site.Visibility = Visibility.Visible; PageStatus.Text = "https://fortnite.gg/ · Original-Webseite · Filter und Zeichnungen werden von Fortnite.GG verwaltet"; }
            };
            browser.ProcessFailed += (_, _) => ShowError("Die Webansicht wurde beendet. Du kannst sie neu laden oder Fortnite.GG im Browser öffnen.");
            ReloadButton.IsEnabled = true;
            browser.Navigate(FortniteGgSite.MapUrl);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            RuntimeButton.Visibility = Visibility.Visible;
            ShowError("Microsoft Edge WebView2 Runtime fehlt. Fortnite.GG lässt sich direkt im Browser nutzen. Für die integrierte Ansicht installiere die kostenlose Runtime von Microsoft.");
            firstNavigation.TrySetResult(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException or InvalidOperationException)
        { ShowError("Die Webansicht konnte nicht gestartet werden. " + ex.Message); firstNavigation.TrySetResult(false); }
    }
    private void ShowError(string text)
    { if (closed) return; Site.Visibility = Visibility.Hidden; ErrorText.Text = text; ErrorPanel.Visibility = Visibility.Visible; PageStatus.Text = "Onlinekarte nicht verfügbar · lokale Planung bleibt nutzbar"; }
    private void Reload(object sender, RoutedEventArgs e)
    {
        try { if (Site.CoreWebView2 is not null) { ErrorPanel.Visibility = Visibility.Collapsed; Site.Visibility = Visibility.Visible; Site.CoreWebView2.Navigate(FortniteGgSite.MapUrl); } }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        { ShowError("Die Webansicht bitte schließen und erneut öffnen. " + ex.Message); }
    }
    private void OpenLink(string address)
    {
        try { ExternalLinks.Open(address); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or System.Security.SecurityException)
        { PageStatus.Text = "Link konnte nicht geöffnet werden: " + address; }
    }
    private void OpenBrowser(object sender, RoutedEventArgs e) => OpenLink(FortniteGgSite.MapUrl);
    private void OpenRuntime(object sender, RoutedEventArgs e) => OpenLink(FortniteGgSite.RuntimeUrl);
    private async void ClearWebData(object sender, RoutedEventArgs e)
    {
        if (Site.CoreWebView2 is null) return;
        try
        {
            await Site.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
            if (!closed) { Site.CoreWebView2.Navigate(FortniteGgSite.MapUrl); PageStatus.Text = "Cookies, Cache und Webdaten dieser Ansicht gelöscht"; }
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        { if (!closed) PageStatus.Text = "Webdaten konnten nicht gelöscht werden: " + ex.Message; }
    }
    private void ReturnToPlanner(object sender, RoutedEventArgs e) => Close();
    private void Minimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
}
