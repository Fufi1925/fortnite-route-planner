using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RoutePlanner.App;
public static class Dialogs
{
    public static MessageBoxResult Show(Window owner,string message,string title,MessageBoxButton buttons = MessageBoxButton.OK)
    {
        var dialog = new Window { Owner = owner, Title = title, Width = 470, SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Style = (Style)Application.Current.FindResource(typeof(Window)), ShowInTaskbar = false };
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = title, FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,16) });
        content.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(160,160,160)), Margin = new Thickness(0,0,0,24) });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var result = MessageBoxResult.Cancel;
        void Add(string label,MessageBoxResult value,bool isDefault = false)
        {
            var button = new Button { Content = label, MinWidth = 80, Margin = new Thickness(8,0,0,0), IsDefault = isDefault };
            button.Click += (_,_) => { result = value; dialog.Close(); }; actions.Children.Add(button);
        }
        if (buttons == MessageBoxButton.OK) Add("Verstanden",MessageBoxResult.OK,true);
        else { Add("Ja",MessageBoxResult.Yes); Add("Nein",MessageBoxResult.No,buttons == MessageBoxButton.YesNo); if (buttons == MessageBoxButton.YesNoCancel) Add("Abbrechen",MessageBoxResult.Cancel,true); }
        content.Children.Add(actions); dialog.Content = content; dialog.ShowDialog(); return result;
    }
}
