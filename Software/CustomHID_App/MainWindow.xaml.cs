using MahApps.Metro.Controls;
using System.Diagnostics;

namespace CustomHID_App
{
    public partial class MainWindow : MetroWindow
    {
        public string LinkName { get; set; } = "A_D Electronics";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://adelectronics.ru/"));
        }
    }
}
