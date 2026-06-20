using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WellBot.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/Icons/wellbot_small.ico");
            var sri = System.Windows.Application.GetResourceStream(uri);
            if (sri != null)
            {
                NotifyIcon.Icon = new System.Drawing.Icon(sri.Stream);
            }
        }
        catch { }
    }
}