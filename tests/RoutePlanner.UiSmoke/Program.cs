using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using RoutePlanner.App;
using RoutePlanner.Core;

namespace RoutePlanner.UiSmoke;
public static class Program
{
    [STAThread]
    public static int Main()
    {
        int exitCode = 1;
        string temp = Path.Combine(Path.GetTempPath(),"route-ui-"+Guid.NewGuid());
        var app = new RoutePlanner.App.App(); app.InitializeComponent(); app.OpenPlannerOnStartup = false;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Dispatcher.BeginInvoke(async () =>
        {
            MainWindow? window = null;
            try
            {
                var store = new LocalStore(temp);
                window = new MainWindow(temp); window.Show();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                // Request a common desktop size; Windows may cap it to the runner's screen limits.
                window.Width = 1440; window.Height = 920; window.UpdateLayout();
                var map = (MapView)window.FindName("PlannerMap");
                Assert(map.Route!.Points.Count == 0 && map.VisibleSpawns.Count == 0 && store.Load().Routes.Count == 0,"Start ohne erfundene Routen, Orte oder Statistiken");
                Assert(!Directory.Exists(Path.Combine(temp,"web-fortnite-gg")),"Fortnite.GG-Profil und Website entstehen erst nach bewusstem Öffnen");
                Assert(((SolidColorBrush)window.Background).Color == Color.FromRgb(8,8,8),"Schwarzer Fensterhintergrund");
                var routes = (ComboBox)window.FindName("RoutesBox");
                Assert(((SolidColorBrush)routes.Background).Color == Color.FromRgb(21,21,21),"Dunkles Auswahlfeld");
                await Capture(window,"planner-empty.png");
                ((TextBox)window.FindName("NameBox")).Text = "Meine Route";
                foreach (var point in new MapPoint[] {new(600,2400),new(900,1900),new(1450,1600),new(1700,950),new(2400,650)})
                {
                    ((TextBox)window.FindName("PointX")).Text = point.X.ToString(CultureInfo.CurrentCulture);
                    ((TextBox)window.FindName("PointY")).Text = point.Y.ToString(CultureInfo.CurrentCulture);
                    Click(window,"Punkt hinzufügen");
                }
                Click(window,"Speichern");
                Assert(store.Load().Routes.Single().Points.Count == 5 && store.Load().Routes.Single().Name == "Meine Route","Echte Wegpunktbearbeitung und Speicherung");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                Assert(Descendants(routes).OfType<TextBlock>().Any(t=>t.Text=="Meine Route") && !Descendants(routes).OfType<TextBlock>().Any(t=>t.Text.StartsWith("RoutePlan {",StringComparison.Ordinal)),"Routenauswahl zeigt den Namen statt technischer Datensätze");
                var originalId = map.Route!.Id;
                window.AddManualMarker(new(910,1890));
                Assert(store.Load().Maps.Single().Spawns.Single().ManuallyConfirmed,"Eigener Ort dauerhaft gespeichert");
                var markerFile = Path.Combine(temp,"markers.json");
                RouteJson.WriteAtomic(markerFile,RouteJson.Serialize(new(){Maps=[new(){Source="Eigene vermessene Koordinaten",Spawns=[new("p",new(1700,960))]}]}));
                window.ImportMarkerFile(markerFile);
                Assert(store.Load().Maps.Single().Spawns.Count == 2 && store.Load().Maps.Single().Spawns.Last().Source == "Eigene vermessene Koordinaten","Ortsimport und Quellenangabe gespeichert");
                var radius = (Slider)window.FindName("RadiusSlider"); radius.Value = 0;
                Assert(map.VisibleSpawns.Count == 0,"Radiusfilter blendet außerhalb liegende Orte aus");
                radius.Value = 110;Click(window,"Speichern");
                Assert(map.VisibleSpawns.Count == 2,"Radiusfilter zeigt passende Orte");
                var area=new SearchArea(800,1800,1000,2000);
                window.SetSearchArea(area);Click(window,"Speichern");
                Assert(map.VisibleSpawns.Count==1 && store.Load().Routes.Single().Area==area,"Bereich filtert vorhandene Orte und wird gespeichert");
                radius.Value=0;Assert(map.VisibleSpawns.Count==1,"Aktiver Bereich ist unabhängig vom Radius");
                ((Button)window.FindName("ClearRegionButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(map.VisibleSpawns.Count==0,"Bereich entfernen aktiviert Radiusfilter wieder");
                radius.Value=110;Click(window,"Speichern");
                await Capture(window,"planner.png");
                window.ExportRouteFile(Path.Combine(temp,"export.json"));
                window.ImportRouteFile(Path.Combine(temp,"export.json"));Click(window,"Speichern");
                Assert(store.Load().Routes.Count == 2 && map.Route!.Id != originalId && map.Route.Points.Count == 5,"Export und Import mit unabhängiger Routen-ID");
                Click(window,"Duplizieren");Click(window,"Speichern");
                Assert(store.Load().Routes.Count == 3,"Routenduplikation");
                Click(window,"Laden");Assert(map.Route!.Points.Count == 5,"Lokale Bibliothek neu geladen");
                var imagePath = Path.Combine(temp,"Eigene Karte.png");WriteImage(imagePath,128,128);
                window.OpenMapFile(imagePath);((TextBox)window.FindName("NameBox")).Text = "Bildroute";Click(window,"Speichern");
                var imageRouteId = map.Route!.Id;
                Assert(map.MapImage is not null && map.Route.MapVersion.StartsWith("MAP-",StringComparison.Ordinal),"Eigenes Kartenbild tatsächlich importiert");
                var colorPath = Path.Combine(temp,"Farben.png");WriteColorImage(colorPath);
                var colorImport = new MapImages(temp).Import(colorPath);
                Assert(HasColor(colorImport.Image) && HasColor(new MapImages(temp).Load(colorImport.Asset)),"Farbige Karte bleibt beim Import, Speichern und Laden farbig");
                var invalid = Path.Combine(temp,"not-image.png");File.WriteAllText(invalid,"not an image at all");
                Reject(()=>new MapImages(temp).Import(invalid));WriteImage(invalid,128,64);Reject(()=>new MapImages(temp).Import(invalid));
                window.Close();window = new MainWindow(temp);window.Show();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                window.Width = 1440;window.Height = 920;window.UpdateLayout();
                routes = (ComboBox)window.FindName("RoutesBox");
                routes.SelectedItem = routes.Items.OfType<RoutePlan>().Single(r=>r.Id == imageRouteId);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                map = (MapView)window.FindName("PlannerMap");
                Assert(map.MapImage is not null,$"Kartenbild nach Neustart mit Hashprüfung geladen (Route {map.Route?.Id}; erwartet {imageRouteId}; Status {((TextBlock)window.FindName("StatusLabel")).Text})");
                routes.SelectedItem = routes.Items.OfType<RoutePlan>().Single(r=>r.Id == originalId);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                map.ZoomBy(1.2);map.Fit();
                await Capture(window,"planner-reloaded.png");
                Click(window,"Route starten  →");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var compact = app.Windows.OfType<CompactWindow>().Single();
                Assert(compact.IsVisible && !window.IsVisible && !compact.Topmost,"Kompaktfenster und sichere Standardeinstellung");
                Assert(((SolidColorBrush)compact.Background).Color == Color.FromRgb(8,8,8),"Schwarzer Kompaktfensterhintergrund");
                var pin = Descendants(compact).OfType<CheckBox>().Single();pin.IsChecked = true;Assert(compact.Topmost,"Normales Topmost");pin.IsChecked = false;
                await Task.Delay(1200);
                Assert(store.Load().Activities.Count == 0,"Kein automatisch erfundener Fortschritt");
                await Capture(compact,"compact.png");
                for (int i=0;i<5;i++) Click(compact,"Wegpunkt erreicht  →");
                var activity = store.Load().Activities.Single();
                Assert(activity.Completed && activity.VisitedPoints == 5 && activity.DurationSeconds >= 1,"Nur manuelle Fortschritte und tatsächliche Laufzeit gespeichert");
                Click(compact,"Zur Planung");Assert(window.IsVisible,"Rückweg aus Kompaktfenster");
                Click(window,"Verlauf öffnen");await Capture(window,"history.png");Click(window,"← Zur Planung");
                Click(window,"+ Neue Route");Click(window,"Speichern");
                Assert(((MapView)window.FindName("PlannerMap")).Route!.Points.Count == 0,"Leere eigene Route nutzbar");
                if(Environment.GetEnvironmentVariable("ROUTE_VERIFY_PUBLIC_MAP")=="1")
                {
                    bool loaded=await window.LoadPublicMapAsync(showErrors:false);
                    Assert(loaded,"Öffentliche Fortnite-Karte tatsächlich per HTTPS geladen: "+((TextBlock)window.FindName("StatusLabel")).Text);
                    map=(MapView)window.FindName("PlannerMap");
                    Assert(map.MapImage is not null && map.VisibleSpawns.Count==0,"Echtes Kartenbild ohne erfundene Truhenpunkte");
                    Assert(HasColor((BitmapSource)map.MapImage!),"Öffentliche Fortnite-Karte behält echte Farben");
                    Assert(((TextBlock)window.FindName("ChestStatusLabel")).Text.Contains("kein Datensatz"),"Fehlende Truhenquelle ausdrücklich sichtbar");
                    window.SetSearchArea(new(900,1100,2200,2400));Click(window,"Speichern");
                    Assert(store.Load().Routes.Single(r=>r.Id==map.Route!.Id).Area is not null,"Bereich auf öffentlicher Karte tatsächlich gespeichert");
                    Assert(((TextBlock)window.FindName("FanNotice")).Visibility==Visibility.Visible,"Epic-Fan-Content-Hinweis sichtbar");
                    await Capture(window,"planner.png");
                    Click(window,"Kompakt");await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    compact=app.Windows.OfType<CompactWindow>().Single();await Capture(compact,"compact.png");Click(compact,"Zur Planung");
                }
                if(Environment.GetEnvironmentVariable("ROUTE_VERIFY_FORTNITE_GG")=="1")
                {
                    Click(window,"Fortnite.GG öffnen ↗");
                    var web=app.Windows.OfType<WebMapWindow>().Single();
                    Assert(await web.InitialNavigation.WaitAsync(TimeSpan.FromSeconds(60)),"Original-Fortnite.GG tatsächlich in WebView2 geladen");
                    var site=(WebView2)web.FindName("Site");var browser=site.CoreWebView2;
                    Assert(!browser.Settings.AreHostObjectsAllowed && !browser.Settings.IsWebMessageEnabled,"Webseite ohne native Hostobjekte und Nachrichtenbrücke");
                    Console.WriteLine("WEBVIEW runtime="+browser.Environment.BrowserVersionString+" source="+browser.Source);
                    // Test-only DOM interaction; the shipped application never injects a script or copies site data.
                    bool filters=false;
                    for(int i=0;i<40;i++)
                    {
                        filters=await browser.ExecuteScriptAsync("['spawns-chests','spawns-buried_chests','spawns-rare_chests'].every(id=>document.getElementById(id))")=="true";
                        if(filters)break;await Task.Delay(500);
                    }
                    Assert(filters,"Originalseite enthält Chests, Buried Chests und Rare Chests");
                    await BrowserClick(browser,"[data-search=spawns] .arrow");
                    await Task.Delay(500);
                    foreach(var (label,id) in new[]{("Chests","spawns-chests"),("Buried Chests","spawns-buried_chests"),("Rare Chests","spawns-rare_chests")})
                    {
                        string target=JsonSerializer.Serialize(id);
                        await BrowserClick(browser,"#"+id+" > span");
                        string activated=await browser.ExecuteScriptAsync("document.getElementById("+target+").classList.contains('selected')");
                        Assert(activated=="true","Sichtbarer Fortnite.GG-Filter bedienbar: "+label);
                    }
                    await Task.Delay(1000);
                    string tiles=await browser.ExecuteScriptAsync("document.querySelectorAll('.leaflet-tile-loaded').length>0");
                    Assert(tiles=="true","Echte farbige Kartenkacheln der Originalseite geladen");
                    await BrowserClick(browser,"[data-tooltip='Draw on map']");
                    Assert(await browser.ExecuteScriptAsync("document.querySelector('[data-tooltip=\"Draw a path\"]').getBoundingClientRect().height>0")=="true","Originales Routen-Zeichenwerkzeug sichtbar und geöffnet");
                    Directory.CreateDirectory("artifacts/ui-smoke");
                    using(var output=File.Create("artifacts/ui-smoke/fortnite-gg.png"))await browser.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,output);
                    Click(web,"Zur lokalen Planung");
                    Assert(window.IsVisible && !app.Windows.OfType<WebMapWindow>().Any(),"Rückweg aus Onlinekarte ohne Verlust der lokalen Route");
                }
                Console.WriteLine("RESULT Windows WPF smoke passed");exitCode = 0;
            }
            catch (Exception ex) { Console.WriteLine("FAIL Windows WPF smoke: "+ex); }
            finally
            {
                if (window is not null) { Click(window,"Speichern");window.Close(); }
                for(int attempt=0;Directory.Exists(temp)&&attempt<30;attempt++)
                {
                    try { Directory.Delete(temp,true); }
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
                    { if(attempt==29)Console.WriteLine("NOTE WebView2-Profil wird auf dem temporären Runner noch freigegeben");else await Task.Delay(500); }
                }
                app.Shutdown(exitCode);
            }
        });
        app.Run();return exitCode;
    }
    private static void Assert(bool condition,string name) { if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name); }
    private static void Reject(Action action) {try{action();}catch(InvalidDataException){Console.WriteLine("PASS Ungültige Bilddatei abgelehnt");return;}throw new Exception("Invalid image accepted");}
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++) {
            var child=VisualTreeHelper.GetChild(parent,i);yield return child;
            foreach(var item in Descendants(child))yield return item;
        }
    }
    private static void Click(Window window,string content) => Descendants(window).OfType<Button>()
        .Single(b=>b.Content?.ToString()==content).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Capture(Window window,string name)
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);
        bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var directory=Path.GetFullPath("artifacts/ui-smoke");Directory.CreateDirectory(directory);
        using var file=File.Create(Path.Combine(directory,name));encoder.Save(file);
    }
    private static void WriteImage(string path,int width,int height)
    {
        var bitmap=BitmapSource.Create(width,height,96,96,PixelFormats.Gray8,null,Enumerable.Range(0,width*height).Select(i=>(byte)(i%128)).ToArray(),width);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
    private static void WriteColorImage(string path)
    {
        byte[] pixels=new byte[64*64*4];
        for(int i=0;i<pixels.Length;i+=4){pixels[i]=30;pixels[i+1]=90;pixels[i+2]=180;pixels[i+3]=255;}
        var bitmap=BitmapSource.Create(64,64,96,96,PixelFormats.Bgra32,null,pixels,64*4);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(path);encoder.Save(output);
    }
    private static bool HasColor(BitmapSource image)
    {
        var bitmap=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
        byte[] pixels=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);
        for(int i=0;i<pixels.Length;i+=4)if(pixels[i]!=pixels[i+1]||pixels[i]!=pixels[i+2])return true;
        return false;
    }
    private static async Task BrowserClick(CoreWebView2 browser,string selector)
    {
        // Real browser input only within this test's WebView2, never Windows/game input.
        string position=await browser.ExecuteScriptAsync("(()=>{let e=document.querySelector("+JsonSerializer.Serialize(selector)+");if(!e)return null;let r=e.getBoundingClientRect();return {x:r.left+r.width/2,y:r.top+r.height/2,height:r.height};})()");
        using var point=JsonDocument.Parse(position);
        if(point.RootElement.ValueKind==JsonValueKind.Null||point.RootElement.GetProperty("height").GetDouble()==0)throw new Exception("Webelement nicht sichtbar: "+selector);
        double x=point.RootElement.GetProperty("x").GetDouble(),y=point.RootElement.GetProperty("y").GetDouble();
        foreach(string type in new[]{"mousePressed","mouseReleased"})
            await browser.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new {type,x,y,button="left",clickCount=1}));
        await Task.Delay(250);
    }
}
