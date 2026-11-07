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
        int launchRequests = 0; bool acceptLaunchRequest = false;
        GameLaunchResult RequestGameForTest() { launchRequests++;return new(acceptLaunchRequest,acceptLaunchRequest ? "Epic-Start angefragt · Test zählt nur den Aufruf, startet kein Spiel" : "Epic-Start fehlgeschlagen · Testfall"); }
        var app = new RoutePlanner.App.App(); app.InitializeComponent(); app.OpenPlannerOnStartup = false;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Dispatcher.BeginInvoke(async () =>
        {
            MainWindow? window = null;
            try
            {
                var store = new LocalStore(temp);
                window = new MainWindow(temp,RequestGameForTest); window.Show();
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
                RouteJson.WriteAtomic(markerFile,RouteJson.Serialize(new(){Maps=[new(){Source="Eigene vermessene Koordinaten",Spawns=[new("p",new(1700,960),Kind:SpawnKind.BuriedChest)]}]}));
                window.ImportMarkerFile(markerFile);
                Assert(store.Load().Maps.Single().Spawns.Count == 2 && store.Load().Maps.Single().Spawns.Last().Source == "Eigene vermessene Koordinaten","Ortsimport und Quellenangabe gespeichert");
                Assert(store.Load().Maps.Single().Spawns.Last().Kind==SpawnKind.BuriedChest,"Ortsimport erhält den ausdrücklich deklarierten Kistentyp");
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
                window.Close();window = new MainWindow(temp,RequestGameForTest);window.Show();
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
                Assert(launchRequests==0,"Ohne ausgewählte Startoption kein Epic-Startauftrag");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var compact = app.Windows.OfType<CompactWindow>().Single();
                Assert(compact.IsVisible && !window.IsVisible && compact.Topmost&&!compact.ShowActivated,"Routenstart öffnet automatisch angeheftetes Fenster ohne Aktivierung");
                Assert(((CheckBox)compact.FindName("PinWindow")).IsChecked==true,"Automatisches Anheften stimmt mit sichtbarem Schalter überein");
                var work=SystemParameters.WorkArea;
                Assert(Math.Abs(compact.Left+compact.ActualWidth-(work.Right-16))<2&&Math.Abs(compact.Top-(work.Top+16))<2,"Routenfenster erscheint tatsächlich oben rechts");
                Assert(((MapView)compact.FindName("CompactMap")).VisibleSpawns.Count==0,"Kompaktfenster zeigt keine Truhen- oder Lootpositionen");
                Assert(((SolidColorBrush)compact.Background).Color == Color.FromRgb(8,8,8),"Schwarzer Kompaktfensterhintergrund");
                await VerifyDesktopVisibility(compact);
                var pin = (CheckBox)compact.FindName("PinWindow");pin.IsChecked = false;Assert(!compact.Topmost,"Automatisches Anheften lässt sich ausschalten");
                pin.IsChecked=true;Assert(compact.Topmost,"Normales Topmost wieder einschalten");
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
                var kindBox=(ComboBox)window.FindName("MarkerKindBox");
                for(int kind=1;kind<=3;kind++){kindBox.SelectedIndex=kind;window.AddManualMarker(new(400+kind*50,400));}
                window.SetSearchArea(new(400,350,600,450));Click(window,"Speichern");
                var chestFilter=(CheckBox)window.FindName("ShowChests");
                var buriedFilter=(CheckBox)window.FindName("ShowBuriedChests");
                var rareFilter=(CheckBox)window.FindName("ShowRareChests");
                ((CheckBox)window.FindName("ShowOtherMarkers")).IsChecked=false;
                for(int mask=0;mask<8;mask++)
                {
                    chestFilter.IsChecked=(mask&1)!=0;buriedFilter.IsChecked=(mask&2)!=0;rareFilter.IsChecked=(mask&4)!=0;
                    Assert(map.VisibleSpawns.Count==System.Numerics.BitOperations.PopCount((uint)mask),"Lokale Kistenfilter bedienen Kombination "+mask);
                }
                chestFilter.IsChecked=false;buriedFilter.IsChecked=true;rareFilter.IsChecked=false;Click(window,"Speichern");
                var filterRouteId=map.Route!.Id;
                Assert(store.Load().Routes.Single(r=>r.Id==filterRouteId).VisibleKinds.SequenceEqual(new[]{SpawnKind.BuriedChest}),"Filterauswahl tatsächlich gespeichert");
                Assert(map.VisibleSpawns.Single().Kind==SpawnKind.BuriedChest,"Eigene Markierungstypen statt umgedeuteter Punkte");
                await Capture(window,"local-chest-filters.png");
                Click(window,"Laden");
                Assert(((CheckBox)window.FindName("ShowBuriedChests")).IsChecked==true&&((CheckBox)window.FindName("ShowChests")).IsChecked==false,"Lokale Filter nach Laden wiederhergestellt");
                ((Button)window.FindName("ClearRegionButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((CheckBox)window.FindName("AllMarkers")).IsChecked=true;
                Assert(map.VisibleSpawns.Count==2&&map.VisibleSpawns.All(p=>p.Kind==SpawnKind.BuriedChest),"Alle-Orte-Modus berücksichtigt weiterhin Kistentypfilter");
                Click(window,"Speichern");
                Click(window,"Route zeichnen");Assert(!map.SelectArea&&!map.PlaceMarkers,"Eigener Routenmodus kehrt aus Bereichs- und Markierungswerkzeugen zurück");
                ((TextBox)window.FindName("PointX")).Text="500";((TextBox)window.FindName("PointY")).Text="400";Click(window,"Punkt hinzufügen");Click(window,"Speichern");
                var launchOption=(CheckBox)window.FindName("LaunchFortniteBox");launchOption.IsChecked=true;
                Click(window,"Route starten  →");
                Assert(launchRequests==1&&!app.Windows.OfType<CompactWindow>().Any()&&window.IsVisible,"Fehlgeschlagener Epic-Start startet keinen Routengang");
                acceptLaunchRequest=true;Click(window,"Route starten  →");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                compact=app.Windows.OfType<CompactWindow>().Single();
                Assert(launchRequests==2&&compact.IsVisible&&compact.Topmost,"Ausgewählte Epic-Startoption sendet genau einen Auftrag und öffnet angeheftetes Routenfenster");
                Assert(((TextBlock)compact.FindName("EpicStatus")).Text.StartsWith("Epic-Start angefragt"),"Kompaktfenster bestätigt nur den Startauftrag und behauptet kein laufendes Spiel");
                Assert(Descendants(compact).OfType<TextBlock>().Any(t=>t.Text.Contains("Leave-Zeitpunkt unbekannt")),"Fehlende Truhenerkennung und Leave-Zeit sichtbar ohne erfundene Empfehlung");
                Click(compact,"Beenden");Click(compact,"Zur Planung");launchOption.IsChecked=false;
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
                    compact=app.Windows.OfType<CompactWindow>().Single();
                    Assert(!compact.Topmost,"Reine Kompaktvorschau ohne Sitzung heftet sich nicht automatisch an");
                    await Capture(compact,"compact.png");Click(compact,"Zur Planung");
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
    private static async Task VerifyDesktopVisibility(CompactWindow compact)
    {
        var cover=new Window {Style=null,Title="Neutrales Testfenster · keine Fortnite-Partie",WindowStyle=WindowStyle.None,
            WindowState=WindowState.Maximized,ResizeMode=ResizeMode.NoResize,Background=new SolidColorBrush(Color.FromRgb(69,69,69)),ShowInTaskbar=false};
        try
        {
            cover.Show();cover.Activate();await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert(cover.IsActive&&!compact.IsActive&&compact.Topmost,"Routenfenster bleibt angeheftet, während neutrales Fenster aktiv ist");
            string output=Path.GetFullPath("artifacts/ui-smoke/desktop-hud.png");Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var start=new System.Diagnostics.ProcessStartInfo("pwsh") {UseShellExecute=false,CreateNoWindow=true};
            foreach(string arg in new[]{"-NoProfile","-NonInteractive","-File",Path.GetFullPath("scripts/capture-test-desktop.ps1"),"-OutputPath",output})start.ArgumentList.Add(arg);
            using var capture=System.Diagnostics.Process.Start(start)!;
            await capture.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert(capture.ExitCode==0&&File.Exists(output),"Tatsächlicher Windows-Desktop statt Fenster-Render für Sichtbarkeitsprüfung aufgenommen");
            var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(output);image.EndInit();
            var pixels=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
            double sx=image.PixelWidth/SystemParameters.VirtualScreenWidth,sy=image.PixelHeight/SystemParameters.VirtualScreenHeight;
            Color Pixel(double x,double y)
            {
                byte[] sample=new byte[4];pixels.CopyPixels(new Int32Rect((int)((x-SystemParameters.VirtualScreenLeft)*sx),(int)((y-SystemParameters.VirtualScreenTop)*sy),1,1),sample,4,0);
                return Color.FromRgb(sample[2],sample[1],sample[0]);
            }
            Assert(Pixel(compact.Left+8,compact.Top+70)==Color.FromRgb(8,8,8),"Routenfenster ist im echten Desktopbild über aktivem neutralen Fenster sichtbar");
            Assert(Pixel(SystemParameters.WorkArea.Left+30,SystemParameters.WorkArea.Top+100)==Color.FromRgb(69,69,69),"Neutrales Testfenster im Desktopbild bestätigt; kein verdeckter Hintergrundtest");
        }
        finally {cover.Close();}
    }
}
