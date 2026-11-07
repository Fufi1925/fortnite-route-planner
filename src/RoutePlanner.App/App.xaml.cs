using System.Windows;

namespace RoutePlanner.App;
public partial class App : Application
{
    public bool OpenPlannerOnStartup { get; set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (OpenPlannerOnStartup) new MainWindow().Show();
    }
}
