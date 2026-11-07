using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RoutePlanner.App;
using RoutePlanner.Core;

namespace RoutePlanner.UiSmoke;
public static class Program
{
    [STAThread]
    public static int Main()
    {
        int exitCode = 1;
        string temp = Path.Combine(Path.GetTempPath(), "route-ui-"+Guid.NewGuid());
        var app = new RoutePlanner.App.App();
        app.InitializeComponent(); app.OpenPlannerOnStartup = false; app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Dispatcher.BeginInvoke(async () =>
        {
            MainWindow? window = null;
            try
            {
                var store = new LocalStore(temp); store.Save(new() { Routes = [DemoData.Example()] });
                window = new MainWindow(temp); window.Show();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var map = (MapView)window.FindName("PlannerMap");
                Assert(map.ActualWidth > 100 && map.Route?.Points.Count == 5, "WPF-Fenster und Karte geladen");
                Assert(window.Background is SolidColorBrush background && background.Color == Color.FromRgb(16,18,22), "Dunkler Fensterhintergrund angewendet");
                Assert(((ComboBox)window.FindName("RoutesBox")).Foreground is SolidColorBrush comboText && comboText.Color == Color.FromRgb(24,32,42), "Lesbarer Auswahltext");
                Capture(window, "planner.png");
                var name = (TextBox)window.FindName("NameBox");name.Text = "UI-Test gespeichert";
                Click(window, "Speichern");
                Assert(store.Load().Routes[0].Name == name.Text, "Speichern über echten Button-Handler");
                var radius = (Slider)window.FindName("RadiusSlider");radius.Value = 0;
                Assert(map.VisibleSpawns.Count == new SpawnIndex(DemoData.Spawns).Query(map.Route!).Count, "Radiusfilter mit Oberfläche verbunden");
                radius.Value = 110;Click(window, "Speichern");
                map.ZoomBy(1.2); map.Fit();
                Assert(map.Route!.Points.Count == 5, "Zoomen/Einpassen erhält Route");
                Click(window, "Laden");
                Assert(((TextBox)window.FindName("NameBox")).Text == "UI-Test gespeichert", "Laden über Button");
                Click(window, "Demo abspielen");
                await Task.Delay(6500);
                Assert(store.Load().DemoHistory.Count == 1, "Demo-Timer, Provider und Statistik gespeichert");
                Click(window, "Kompaktansicht ↗");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var compact = app.Windows.OfType<CompactWindow>().Single();
                Assert(compact.IsVisible && !window.IsVisible && !compact.Topmost, "Kompaktansicht, standardmäßig nicht Topmost");
                Assert(compact.Background is SolidColorBrush compactBackground && compactBackground.Color == Color.FromRgb(16,18,22), "Dunkler Kompaktfensterhintergrund angewendet");
                var topmost = Descendants(compact).OfType<CheckBox>().Single();
                topmost.IsChecked = true;Assert(compact.Topmost, "Normales Topmost aktiviert");
                topmost.IsChecked = false;Capture(compact, "compact.png");
                Click(compact, "Zur Planung");
                Assert(window.IsVisible, "Zuverlässiger Weg zurück zur Planung");
                Click(window, "Neu");Click(window, "Speichern");
                Assert(((MapView)window.FindName("PlannerMap")).Route!.Points.Count == 0, "Leere Karte speicherbar");
                Console.WriteLine("RESULT Windows WPF smoke passed");exitCode = 0;
            }
            catch (Exception ex) { Console.WriteLine("FAIL Windows WPF smoke: " + ex); }
            finally
            {
                // Clear any dirty state by saving through the regular app action before closing.
                if (window is not null) { Click(window,"Speichern");window.Close(); }
                if (Directory.Exists(temp)) Directory.Delete(temp,true);
                app.Shutdown(exitCode);
            }
        });
        app.Run();return exitCode;
    }
    private static void Assert(bool condition,string name)
    { if (!condition) throw new Exception(name);Console.WriteLine("PASS "+name); }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
        {
            var child=VisualTreeHelper.GetChild(parent,i);yield return child;
            foreach(var item in Descendants(child))yield return item;
        }
    }
    private static void Click(Window window,string content) => Descendants(window).OfType<Button>()
        .Single(b=>b.Content?.ToString()==content).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Capture(Window window,string name)
    {
        window.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);
        bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var directory=Path.GetFullPath("artifacts/ui-smoke");Directory.CreateDirectory(directory);
        using var file=File.Create(Path.Combine(directory,name));encoder.Save(file);
    }
}
